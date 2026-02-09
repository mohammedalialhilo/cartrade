using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class FinanceUpdateViewModel
{
    public int VehicleId { get; set; }

    [Display(Name = "Skick (A-E)")]
    [StringLength(16)]
    public string? ConditionGrade { get; set; }

    [Display(Name = "Intern värdering")]
    [Range(0, 10_000_000)]
    public decimal InternalValuation { get; set; }

    [Display(Name = "Offertpris")]
    [Range(0, 10_000_000)]
    public decimal OfferPrice { get; set; }

    [Display(Name = "Leverantör accepterade")]
    public bool? SupplierAccepted { get; set; }

    [Display(Name = "Kommentar")]
    [StringLength(4000)]
    public string? Notes { get; set; }
}
