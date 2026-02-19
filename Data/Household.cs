using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class Household
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsChoreMutationsLocked { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
