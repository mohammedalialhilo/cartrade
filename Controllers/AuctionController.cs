using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize]
public class AuctionController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTimeOffset.UtcNow;
        var allOpenListings = await context.AuctionListings
            .Include(a => a.Vehicle)
            .Include(a => a.Bids)
            .Where(a => !a.IsClosed && a.Vehicle.Status == VehicleStatus.InAuction)
            .ToListAsync();

        var listings = allOpenListings
            .Where(a => a.EndsAt > now)
            .OrderBy(a => a.EndsAt)
            .ToList();

        return View(listings);
    }

    public async Task<IActionResult> Details(int id)
    {
        var listing = await context.AuctionListings
            .Include(a => a.Vehicle)
            .Include(a => a.Bids)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (listing is null)
        {
            return NotFound();
        }

        ViewData["BidModel"] = new PlaceBidViewModel { AuctionListingId = listing.Id };
        return View(listing);
    }

    [Authorize(Roles = $"{SeedData.AdminRole},{SeedData.DealerRole}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(PlaceBidViewModel model)
    {
        var listing = await context.AuctionListings
            .Include(a => a.Bids)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == model.AuctionListingId);

        if (listing is null)
        {
            return NotFound();
        }

        if (listing.IsClosed || listing.EndsAt <= DateTimeOffset.UtcNow || listing.Vehicle.Status != VehicleStatus.InAuction)
        {
            TempData["Error"] = "Auktionen är stängd.";
            return RedirectToAction(nameof(Details), new { id = model.AuctionListingId });
        }

        var currentHighest = listing.Bids.OrderByDescending(b => b.Amount).FirstOrDefault()?.Amount ?? 0;
        if (model.Amount <= currentHighest)
        {
            TempData["Error"] = $"Budet måste vara högre än nuvarande högsta bud ({currentHighest:C0}).";
            return RedirectToAction(nameof(Details), new { id = model.AuctionListingId });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var bid = new AuctionBid
        {
            AuctionListingId = listing.Id,
            DealerUserId = user.Id,
            Amount = model.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await context.AuctionBids.AddAsync(bid);
        await context.SaveChangesAsync();

        TempData["Success"] = "Bud registrerat.";
        return RedirectToAction(nameof(Details), new { id = model.AuctionListingId });
    }
}
