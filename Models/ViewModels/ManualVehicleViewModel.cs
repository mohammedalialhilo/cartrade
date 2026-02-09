using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class ManualVehicleViewModel
{
    [Display(Name = "Extern referens")]
    [StringLength(64)]
    public string? ExternalReference { get; set; }

    [Required]
    [Display(Name = "Registreringsnummer")]
    [StringLength(16)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Display(Name = "VIN")]
    [StringLength(32)]
    public string? Vin { get; set; }

    [Required]
    [Display(Name = "Märke")]
    [StringLength(64)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Modell")]
    [StringLength(64)]
    public string Model { get; set; } = string.Empty;

    [Display(Name = "Årsmodell")]
    [Range(1950, 2100)]
    public int? ModelYear { get; set; }

    [Display(Name = "Körsträcka (km)")]
    [Range(0, int.MaxValue)]
    public int OdometerKm { get; set; }

    [Display(Name = "Färg")]
    [StringLength(32)]
    public string? Color { get; set; }

    [Display(Name = "Drivmedel")]
    [StringLength(32)]
    public string? FuelType { get; set; }

    [Display(Name = "Samarbetspartner")]
    [StringLength(128)]
    public string? PartnerName { get; set; }
}
