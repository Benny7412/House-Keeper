using HouseKeeper.Components.Features.Chores.Contracts;
using HouseKeeper.Components.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Data;
using MongoDB.Driver;

namespace HouseKeeper.Components.Features.Chores.Services;

public sealed class ChoresService
{
    private readonly MongoDbContext _dbContext;
    private readonly HouseholdContextAccessor _householdContextAccessor;

    public ChoresService(MongoDbContext dbContext, HouseholdContextAccessor householdContextAccessor)
    {
        _dbContext = dbContext;
        _householdContextAccessor = householdContextAccessor;
    }

    public async Task<IReadOnlyList<ChoreVm>> GetAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);

        var chores = await _dbContext.Chores
            .Find(x => x.HouseholdId == context.HouseholdId)
            .ToListAsync(cancellationToken);

        return chores
            .OrderBy(x => x.IsCompleted)
            .ThenByDescending(x => x.IsUrgent)
            .ThenBy(x => x.DueAt ?? DateTimeOffset.MaxValue)
            .Select(MapToVm)
            .ToList();
    }

    public async Task<ChoreVm> CreateAsync(CreateChoreRequest request, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        EnsureChoreMutationsAllowed(context);

        var item = new ChoreItem
        {
            HouseholdId = context.HouseholdId,
            Title = request.Title.Trim(),
            IsUrgent = request.IsUrgent,
            DueAt = request.DueDate is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc), TimeSpan.Zero),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = context.UserId
        };

        await _dbContext.Chores.InsertOneAsync(item, cancellationToken: cancellationToken);
        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = $"{context.DisplayName} added a chore: {item.Title}.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);

        return MapToVm(item);
    }

    public async Task<ChoreVm?> ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);
        EnsureChoreMutationsAllowed(context);

        var item = await _dbContext.Chores
            .Find(x => x.Id == id.ToString("D") && x.HouseholdId == context.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.IsCompleted = !item.IsCompleted;
        await _dbContext.Chores.ReplaceOneAsync(
            x => x.Id == item.Id && x.HouseholdId == context.HouseholdId,
            item,
            cancellationToken: cancellationToken);

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = item.IsCompleted
                ? $"{context.DisplayName} completed a chore: {item.Title}."
                : $"{context.DisplayName} reopened a chore: {item.Title}.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);

        return MapToVm(item);
    }

    private static void EnsureChoreMutationsAllowed(HouseholdContext context)
    {
        if (context.IsChoreMutationsLocked && !context.IsAdmin)
        {
            throw new InvalidOperationException("Only household admins can add or edit chores while chore lock is enabled.");
        }
    }

    private static ChoreVm MapToVm(ChoreItem item)
    {
        if (!Guid.TryParse(item.Id, out var parsedId))
        {
            throw new InvalidOperationException("Chore id is malformed in database.");
        }

        return new ChoreVm
        {
            Id = parsedId,
            Title = item.Title,
            IsUrgent = item.IsUrgent,
            IsCompleted = item.IsCompleted,
            DueAt = item.DueAt,
            CreatedAt = item.CreatedAt
        };
    }
}
