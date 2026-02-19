using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class HouseholdInvitation
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string HouseholdId { get; set; } = string.Empty;
    public string InvitedUserId { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
}
