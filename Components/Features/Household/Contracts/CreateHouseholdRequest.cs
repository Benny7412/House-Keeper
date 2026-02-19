using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Household.Contracts;

public sealed class CreateHouseholdRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
