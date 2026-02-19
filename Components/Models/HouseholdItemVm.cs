public enum ItemType { Chore, Expense }

public record HouseholdItemVm
{
  public Guid Id { get; init; }
  public ItemType Type { get; init; }

  public string Title { get; init; } = "";
  public List<Guid> AssignedToIds { get; init; } = new();

  public bool IsUrgent { get; init; }
  public bool IsCompleted { get; init; } // chore completed, or expense settled

  public DateTimeOffset CreatedAt { get; init; }
  public DateTimeOffset UpdatedAt { get; init; }

  // shared can be null dates
  public DateTimeOffset? DueAt { get; init; }

  // chore-only
  public int? Points { get; init; }
  public DateTimeOffset? CooldownUntil { get; init; }

  // expense-only
  public decimal? Amount { get; init; }
  public string? Currency { get; init; }
  public Guid? PaidById { get; init; }
}
