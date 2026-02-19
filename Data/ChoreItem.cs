using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class ChoreItem
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string HouseholdId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
}
