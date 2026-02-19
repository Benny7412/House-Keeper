using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Expenses.Contracts;

public sealed class CreateExpenseRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    public bool IsUrgent { get; set; }
}
