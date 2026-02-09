using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartrade.Models;

public class InspectionReport
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    [StringLength(4000)]
    public string? Comments { get; set; }

    [StringLength(4000)]
    public string? DamageNotes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal EstimatedRepairCost { get; set; }

    [StringLength(256)]
    public string? InspectorUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
