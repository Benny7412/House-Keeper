using MongoDB.Driver;

namespace HouseKeeper.Data;

public sealed class MongoIndexInitializer
{
    private readonly MongoDbContext _dbContext;

    public MongoIndexInitializer(MongoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ApplicationUser>(
                Builders<ApplicationUser>.IndexKeys.Ascending(x => x.NormalizedUsername),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ApplicationUser>(
                Builders<ApplicationUser>.IndexKeys.Ascending(x => x.NormalizedEmail),
                new CreateIndexOptions { Unique = true })
        ], cancellationToken);

        await _dbContext.HouseholdMemberships.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<HouseholdMembership>(
                Builders<HouseholdMembership>.IndexKeys
                    .Ascending(x => x.HouseholdId)
                    .Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<HouseholdMembership>(
                Builders<HouseholdMembership>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true })
        ], cancellationToken);

        await _dbContext.HouseholdActivities.Indexes.CreateOneAsync(
            new CreateIndexModel<HouseholdActivity>(
                Builders<HouseholdActivity>.IndexKeys
                    .Ascending(x => x.HouseholdId)
                    .Descending(x => x.OccurredAt)),
            cancellationToken: cancellationToken);

        await _dbContext.HouseholdInvitations.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<HouseholdInvitation>(
                Builders<HouseholdInvitation>.IndexKeys
                    .Ascending(x => x.HouseholdId)
                    .Descending(x => x.CreatedAt)),
            new CreateIndexModel<HouseholdInvitation>(
                Builders<HouseholdInvitation>.IndexKeys.Ascending(x => x.InvitedUserId),
                new CreateIndexOptions<HouseholdInvitation>
                {
                    Unique = true,
                    PartialFilterExpression = Builders<HouseholdInvitation>.Filter.Eq(
                        x => x.AcceptedAt,
                        (DateTimeOffset?)null)
                })
        ], cancellationToken);

        await _dbContext.Chores.Indexes.CreateOneAsync(
            new CreateIndexModel<ChoreItem>(
                Builders<ChoreItem>.IndexKeys.Ascending(x => x.HouseholdId)),
            cancellationToken: cancellationToken);

        await _dbContext.Expenses.Indexes.CreateOneAsync(
            new CreateIndexModel<ExpenseItem>(
                Builders<ExpenseItem>.IndexKeys.Ascending(x => x.HouseholdId)),
            cancellationToken: cancellationToken);
    }
}
