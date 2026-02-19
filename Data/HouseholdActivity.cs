using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class HouseholdActivity
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string HouseholdId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
