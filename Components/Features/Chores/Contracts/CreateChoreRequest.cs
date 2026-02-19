using System.ComponentModel.DataAnnotations;

namespace HouseKeeper.Components.Features.Chores.Contracts;

public sealed class CreateChoreRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    public bool IsUrgent { get; set; }

    public DateTime? DueDate { get; set; }
}
