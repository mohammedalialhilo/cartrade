using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartrade.Models;

public class AuctionBid
{
    public int Id { get; set; }

    public int AuctionListingId { get; set; }
    public AuctionListing AuctionListing { get; set; } = default!;

    [StringLength(256)]
    public string DealerUserId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
