using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class PublishAuctionViewModel
{
    public int VehicleId { get; set; }

    [Display(Name = "Reservationspris")]
    [Range(0, 10_000_000)]
    public decimal ReservePrice { get; set; }

    [Display(Name = "Auktion avslutas")]
    public DateTimeOffset EndsAt { get; set; } = DateTimeOffset.UtcNow.AddDays(3);

    [Display(Name = "Notering")]
    [StringLength(4000)]
    public string? Notes { get; set; }
}
