using HouseKeeper.Components.Features.Household.Contracts;
using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;
using HouseKeeper.Data;
using MongoDB.Driver;

namespace HouseKeeper.Components.Features.Household.Services;

public sealed class HouseholdService
{
    private readonly MongoDbContext _dbContext;
    private readonly HouseholdContextAccessor _householdContextAccessor;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public HouseholdService(
        MongoDbContext dbContext,
        HouseholdContextAccessor householdContextAccessor,
        CurrentUserAccessor currentUserAccessor)
    {
        _dbContext = dbContext;
        _householdContextAccessor = householdContextAccessor;
        _currentUserAccessor = currentUserAccessor;
    }

    public sealed record HouseholdSnapshot(
        string HouseholdName,
        bool IsCurrentUserAdmin,
        bool IsChoreMutationsLocked,
        IReadOnlyList<HouseholdMemberVm> Members);

    public async Task<HouseholdSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserAccessor.GetRequiredUserIdAsync();
        var membership = await _dbContext.HouseholdMemberships
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return null;
        }

        var household = await _dbContext.Households
            .Find(x => x.Id == membership.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        if (household is null)
        {
            return null;
        }

        var context = new HouseholdContext(
            membership.HouseholdId,
            household.Name,
            userId,
            string.Empty,
            membership.IsAdmin,
            household.IsChoreMutationsLocked);

        var members = await GetMembersAsync(context, cancellationToken);
        return new HouseholdSnapshot(household.Name, membership.IsAdmin, household.IsChoreMutationsLocked, members);
    }

    public async Task<IReadOnlyList<HouseholdInviteVm>> GetPendingInvitesAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserAccessor.GetRequiredUserIdAsync();
        var invites = await _dbContext.HouseholdInvitations
            .Find(x => x.InvitedUserId == userId && x.AcceptedAt == null)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (invites.Count == 0)
        {
            return [];
        }

        var householdIds = invites
            .Select(x => x.HouseholdId)
            .Distinct()
            .ToList();

        var households = householdIds.Count == 0
            ? []
            : await _dbContext.Households
                .Find(Builders<HouseKeeper.Data.Household>.Filter.In(x => x.Id, householdIds))
                .ToListAsync(cancellationToken);
        var householdNamesById = households.ToDictionary(x => x.Id, x => x.Name);

        return invites.Select(x =>
        {
            if (!Guid.TryParse(x.Id, out var inviteId))
            {
                throw new InvalidOperationException("Invitation id is malformed in database.");
            }

            return new HouseholdInviteVm
            {
                Id = inviteId,
                HouseholdName = householdNamesById.TryGetValue(x.HouseholdId, out var name) ? name : "Unknown household",
                InvitedAt = x.CreatedAt
            };
        }).ToList();
    }

    public async Task<string> GetHouseholdNameAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        return context.HouseholdName;
    }

    public async Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        return context.IsAdmin;
    }

    public async Task<IReadOnlyList<HouseholdMemberVm>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        return await GetMembersAsync(context, cancellationToken);
    }

    public async Task CreateHouseholdAsync(CreateHouseholdRequest request, CancellationToken cancellationToken = default)
    {
        var householdName = request.Name.Trim();
        if (householdName.Length is < 2 or > 80)
        {
            throw new InvalidOperationException("Household name must be between 2 and 80 characters.");
        }

        var userId = await _currentUserAccessor.GetRequiredUserIdAsync();
        var existingMembership = await _dbContext.HouseholdMemberships
            .Find(x => x.UserId == userId)
            .AnyAsync(cancellationToken);

        if (existingMembership)
        {
            throw new InvalidOperationException("This account is already linked to a household.");
        }

        var principal = await _currentUserAccessor.GetRequiredPrincipalAsync();
        var username = principal.Identity?.Name ?? "Someone";

        var household = new HouseKeeper.Data.Household
        {
            Name = householdName,
            CreatedByUserId = userId,
            IsChoreMutationsLocked = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var membership = new HouseholdMembership
        {
            HouseholdId = household.Id,
            UserId = userId,
            IsAdmin = true,
            JoinedAt = DateTimeOffset.UtcNow
        };

        var createdHouseholdActivity = new HouseholdActivity
        {
            HouseholdId = household.Id,
            Message = $"{username} created the household.",
            OccurredAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _dbContext.Households.InsertOneAsync(household, cancellationToken: cancellationToken);
            await _dbContext.HouseholdMemberships.InsertOneAsync(membership, cancellationToken: cancellationToken);
            await _dbContext.HouseholdActivities.InsertOneAsync(createdHouseholdActivity, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("This account is already linked to a household.");
        }
    }

    private async Task<IReadOnlyList<HouseholdMemberVm>> GetMembersAsync(HouseholdContext context, CancellationToken cancellationToken)
    {
        var memberships = await _dbContext.HouseholdMemberships
            .Find(x => x.HouseholdId == context.HouseholdId)
            .ToListAsync(cancellationToken);

        var userIds = memberships.Select(x => x.UserId).ToList();
        var users = userIds.Count == 0
            ? []
            : await _dbContext.Users
                .Find(Builders<ApplicationUser>.Filter.In(x => x.Id, userIds))
                .ToListAsync(cancellationToken);
        var usersById = users.ToDictionary(x => x.Id, x => x);

        return memberships
            .OrderByDescending(x => x.IsAdmin)
            .ThenBy(x => usersById.TryGetValue(x.UserId, out var user) ? user.Username : "Unknown user")
            .Select(x =>
            {
                if (!Guid.TryParse(x.Id, out var parsedMembershipId))
                {
                    throw new InvalidOperationException("Membership id is malformed in database.");
                }

                usersById.TryGetValue(x.UserId, out var user);
                var displayName = string.IsNullOrWhiteSpace(user?.DisplayName)
                    ? user?.Username ?? "Unknown user"
                    : user.DisplayName;

                return new HouseholdMemberVm
                {
                    Id = parsedMembershipId,
                    DisplayName = displayName,
                    IsCurrentUser = x.UserId == context.UserId,
                    IsAdmin = x.IsAdmin
                };
            })
            .ToList();
    }

    public async Task InviteMemberAsync(InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        if (!context.IsAdmin)
        {
            throw new InvalidOperationException("Only household admins can invite members.");
        }

        var username = request.Username.Trim();
        var normalizedUserName = NormalizeName(username);
        var invitedUser = await _dbContext.Users
            .Find(x => x.NormalizedUsername == normalizedUserName)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitedUser is null)
        {
            throw new InvalidOperationException("No account was found with that username.");
        }

        if (invitedUser.Id == context.UserId)
        {
            throw new InvalidOperationException("You cannot invite yourself.");
        }

        var existingMembership = await _dbContext.HouseholdMemberships
            .Find(x => x.UserId == invitedUser.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingMembership is not null && existingMembership.HouseholdId != context.HouseholdId)
        {
            throw new InvalidOperationException("That user is already in another household.");
        }

        if (existingMembership is not null)
        {
            throw new InvalidOperationException("That user is already in this household.");
        }

        var pendingInvitation = await _dbContext.HouseholdInvitations
            .Find(x => x.InvitedUserId == invitedUser.Id && x.AcceptedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingInvitation is not null && pendingInvitation.HouseholdId != context.HouseholdId)
        {
            throw new InvalidOperationException("That user already has a pending invite to another household.");
        }

        if (pendingInvitation is not null)
        {
            throw new InvalidOperationException("That user already has a pending invite to this household.");
        }

        var invitation = new HouseholdInvitation
        {
            HouseholdId = context.HouseholdId,
            InvitedUserId = invitedUser.Id,
            InvitedByUserId = context.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _dbContext.HouseholdInvitations.InsertOneAsync(invitation, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("That user already has a pending invite.");
        }

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = $"{invitedUser.Username} was invited to the household.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task AcceptInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserAccessor.GetRequiredUserIdAsync();
        var existingMembership = await _dbContext.HouseholdMemberships
            .Find(x => x.UserId == userId)
            .AnyAsync(cancellationToken);

        if (existingMembership)
        {
            throw new InvalidOperationException("This account is already linked to a household.");
        }

        var invitation = await _dbContext.HouseholdInvitations
            .Find(x => x.Id == inviteId.ToString("D") && x.InvitedUserId == userId && x.AcceptedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation is null)
        {
            throw new InvalidOperationException("That invite is no longer available.");
        }

        var household = await _dbContext.Households
            .Find(x => x.Id == invitation.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        if (household is null)
        {
            throw new InvalidOperationException("The household for this invite no longer exists.");
        }

        var principal = await _currentUserAccessor.GetRequiredPrincipalAsync();
        var username = principal.Identity?.Name ?? "Someone";

        var membership = new HouseholdMembership
        {
            HouseholdId = invitation.HouseholdId,
            UserId = userId,
            IsAdmin = false
        };

        try
        {
            await _dbContext.HouseholdMemberships.InsertOneAsync(membership, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("This account is already linked to a household.");
        }

        await _dbContext.HouseholdInvitations.UpdateOneAsync(
            x => x.Id == invitation.Id && x.InvitedUserId == userId && x.AcceptedAt == null,
            Builders<HouseholdInvitation>.Update.Set(x => x.AcceptedAt, DateTimeOffset.UtcNow),
            cancellationToken: cancellationToken);

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = invitation.HouseholdId,
            Message = $"{username} joined the household.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task LeaveHouseholdAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        if (context.IsAdmin)
        {
            throw new InvalidOperationException("Household admins cannot leave. Transfer admin first.");
        }

        var deleteResult = await _dbContext.HouseholdMemberships.DeleteOneAsync(
            x => x.HouseholdId == context.HouseholdId && x.UserId == context.UserId,
            cancellationToken);

        if (deleteResult.DeletedCount == 0)
        {
            throw new InvalidOperationException("Your household membership was not found.");
        }

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = $"{context.DisplayName} left the household.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task SetChoreLockAsync(bool isLocked, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        if (!context.IsAdmin)
        {
            throw new InvalidOperationException("Only household admins can update chore lock settings.");
        }

        await _dbContext.Households.UpdateOneAsync(
            x => x.Id == context.HouseholdId,
            Builders<HouseKeeper.Data.Household>.Update.Set(x => x.IsChoreMutationsLocked, isLocked),
            cancellationToken: cancellationToken);

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = isLocked
                ? $"{context.DisplayName} enabled chore lock."
                : $"{context.DisplayName} disabled chore lock.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);
    }

    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();
}
