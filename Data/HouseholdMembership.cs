using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class HouseholdMembership
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string HouseholdId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
