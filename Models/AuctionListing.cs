using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartrade.Models;

public class AuctionListing
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal ReservePrice { get; set; }

    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndsAt { get; set; }

    public bool IsClosed { get; set; }

    [StringLength(256)]
    public string? WinnerDealerUserId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10_000_000)]
    public decimal? SoldPrice { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    public ICollection<AuctionBid> Bids { get; set; } = new List<AuctionBid>();
}
