using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartrade.Models;

public class FinanceEvaluation
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    [StringLength(16)]
    public string? ConditionGrade { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal InternalValuation { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal OfferPrice { get; set; }

    public bool? SupplierAccepted { get; set; }

    public DateTimeOffset? DecisionDate { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    [StringLength(256)]
    public string? FinanceUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
