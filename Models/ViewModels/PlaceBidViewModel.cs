using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class PlaceBidViewModel
{
    public int AuctionListingId { get; set; }

    [Display(Name = "Ditt bud")]
    [Range(0, 10_000_000)]
    public decimal Amount { get; set; }
}
