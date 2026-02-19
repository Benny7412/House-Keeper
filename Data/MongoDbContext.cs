using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HouseKeeper.Data;

public sealed class MongoDbContext
{
    public MongoDbContext(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var dbName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME");
        if (string.IsNullOrWhiteSpace(dbName))
        {
            dbName = options.Value.DatabaseName;
        }

        Database = mongoClient.GetDatabase(dbName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<ApplicationUser> Users => Database.GetCollection<ApplicationUser>("users");
    public IMongoCollection<Household> Households => Database.GetCollection<Household>("households");
    public IMongoCollection<HouseholdMembership> HouseholdMemberships =>
        Database.GetCollection<HouseholdMembership>("householdMemberships");
    public IMongoCollection<ChoreItem> Chores => Database.GetCollection<ChoreItem>("chores");
    public IMongoCollection<ExpenseItem> Expenses => Database.GetCollection<ExpenseItem>("expenses");
    public IMongoCollection<HouseholdActivity> HouseholdActivities =>
        Database.GetCollection<HouseholdActivity>("householdActivities");
    public IMongoCollection<HouseholdInvitation> HouseholdInvitations =>
        Database.GetCollection<HouseholdInvitation>("householdInvitations");
}
