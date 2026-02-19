using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Household.Contracts;

public sealed class InviteMemberRequest
{
    [Required]
    [StringLength(40, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;
}
