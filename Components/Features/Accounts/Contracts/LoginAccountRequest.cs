using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Accounts.Contracts;

public sealed class LoginAccountRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
