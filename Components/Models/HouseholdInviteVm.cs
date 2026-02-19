namespace HouseKeeper.Components.Models;

public sealed record HouseholdInviteVm
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string HouseholdName { get; init; } = string.Empty;
    public DateTimeOffset InvitedAt { get; init; } = DateTimeOffset.UtcNow;
}
