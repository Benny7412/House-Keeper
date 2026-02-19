using System.Security.Claims;
using HouseKeeper.Data;
using MongoDB.Driver;

namespace HouseKeeper.Components.Services;

public sealed record HouseholdContext(
    string HouseholdId,
    string HouseholdName,
    string UserId,
    string DisplayName,
    bool IsAdmin,
    bool IsChoreMutationsLocked);

public sealed class HouseholdContextAccessor
{
    private readonly MongoDbContext _dbContext;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public HouseholdContextAccessor(MongoDbContext dbContext, CurrentUserAccessor currentUserAccessor)
    {
        _dbContext = dbContext;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<HouseholdContext> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        var principal = await _currentUserAccessor.GetRequiredPrincipalAsync();
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Authenticated user id was not found.");
        }

        // membership lookup is the boundary that scopes every household query in feature services
        var membership = await _dbContext.HouseholdMemberships
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            throw new InvalidOperationException("No household is linked to this account yet.");
        }

        var household = await _dbContext.Households
            .Find(x => x.Id == membership.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        if (household is null)
        {
            throw new InvalidOperationException("The household linked to this account no longer exists.");
        }

        return new HouseholdContext(
            membership.HouseholdId,
            household.Name,
            userId,
            principal.Identity?.Name ?? "Someone",
            membership.IsAdmin,
            household.IsChoreMutationsLocked);
    }
}
