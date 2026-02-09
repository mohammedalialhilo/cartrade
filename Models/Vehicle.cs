using Cartrade.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models;

public class Vehicle
{
    public int Id { get; set; }

    [StringLength(64)]
    public string? ExternalReference { get; set; }

    [Required]
    [StringLength(16)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Vin { get; set; }

    [Required]
    [StringLength(64)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Model { get; set; } = string.Empty;

    [Range(1950, 2100)]
    public int? ModelYear { get; set; }

    [Range(0, int.MaxValue)]
    public int OdometerKm { get; set; }

    [StringLength(32)]
    public string? Color { get; set; }

    [StringLength(32)]
    public string? FuelType { get; set; }

    [StringLength(128)]
    public string? PartnerName { get; set; }

    public VehicleSource Source { get; set; } = VehicleSource.ManualEntry;

    public VehicleStatus Status { get; set; } = VehicleStatus.Registered;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(256)]
    public string? CreatedByUserId { get; set; }

    public InspectionReport? InspectionReport { get; set; }

    public FinanceEvaluation? FinanceEvaluation { get; set; }

    public AuctionListing? AuctionListing { get; set; }
}
