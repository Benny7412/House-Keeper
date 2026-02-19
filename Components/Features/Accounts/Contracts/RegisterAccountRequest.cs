using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Accounts.Contracts;

public sealed class RegisterAccountRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 8)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
