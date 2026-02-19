using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;
using HouseKeeper.Data;
using MongoDB.Driver;

namespace HouseKeeper.Components.Features.TownHall.Services;

public sealed class TownHallService
{
    private readonly MongoDbContext _dbContext;
    private readonly HouseholdContextAccessor _householdContextAccessor;

    public TownHallService(MongoDbContext dbContext, HouseholdContextAccessor householdContextAccessor)
    {
        _dbContext = dbContext;
        _householdContextAccessor = householdContextAccessor;
    }

    public async Task<IReadOnlyList<HouseholdActivityVm>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);

        var activities = await _dbContext.HouseholdActivities
            .Find(x => x.HouseholdId == context.HouseholdId)
            .SortByDescending(x => x.OccurredAt)
            .Limit(100)
            .ToListAsync(cancellationToken);

        return activities.Select(x =>
        {
            if (!Guid.TryParse(x.Id, out var parsedId))
            {
                throw new InvalidOperationException("Activity id is malformed in database.");
            }

            return new HouseholdActivityVm
            {
                Id = parsedId,
                Message = x.Message,
                OccurredAt = x.OccurredAt
            };
        }).ToList();
    }
}
