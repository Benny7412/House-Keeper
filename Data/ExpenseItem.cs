using MongoDB.Bson.Serialization.Attributes;

namespace HouseKeeper.Data;

public sealed class ExpenseItem
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string HouseholdId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsSettled { get; set; }
    public bool IsUrgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
}
