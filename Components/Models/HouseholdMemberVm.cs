namespace HouseKeeper.Components.Models;

public sealed record HouseholdMemberVm
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DisplayName { get; init; } = string.Empty;
    public bool IsCurrentUser { get; init; }
    public bool IsAdmin { get; init; }
}
