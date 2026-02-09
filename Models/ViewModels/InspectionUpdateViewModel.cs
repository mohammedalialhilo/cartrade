using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class InspectionUpdateViewModel
{
    public int VehicleId { get; set; }

    [Display(Name = "Startdatum besiktning")]
    public DateTimeOffset? StartedAt { get; set; }

    [Display(Name = "Slutdatum besiktning")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Display(Name = "Kommentarer")]
    [StringLength(4000)]
    public string? Comments { get; set; }

    [Display(Name = "Skador")]
    [StringLength(4000)]
    public string? DamageNotes { get; set; }

    [Display(Name = "Uppskattad reparationskostnad")]
    [Range(0, 10_000_000)]
    public decimal EstimatedRepairCost { get; set; }
}
