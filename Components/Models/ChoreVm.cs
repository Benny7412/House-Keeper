namespace HouseKeeper.Components.Models;

public sealed record ChoreVm
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public bool IsUrgent { get; init; }
    public bool IsCompleted { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
