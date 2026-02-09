using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize(Roles = $"{SeedData.AdminRole},{SeedData.SalesRole}")]
public class SalesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vehicles = await context.Vehicles
            .Include(v => v.AuctionListing)
                .ThenInclude(a => a!.Bids)
            .Where(v => v.Status == VehicleStatus.ReadyForSale ||
                        v.Status == VehicleStatus.InAuction ||
                        v.Status == VehicleStatus.Sold)
            .ToListAsync();

        vehicles = vehicles
            .OrderBy(v => v.Status)
            .ThenByDescending(v => v.CreatedAt)
            .ToList();

        return View(vehicles);
    }

    public async Task<IActionResult> Publish(int id)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.AuctionListing)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null)
        {
            return NotFound();
        }

        if (vehicle.Status != VehicleStatus.ReadyForSale && vehicle.Status != VehicleStatus.InAuction)
        {
            TempData["Error"] = "Fordonet kan inte publiceras för auktion i nuvarande status.";
            return RedirectToAction(nameof(Index));
        }

        var model = new PublishAuctionViewModel
        {
            VehicleId = vehicle.Id,
            ReservePrice = vehicle.FinanceEvaluation?.OfferPrice ?? 0,
            EndsAt = DateTimeOffset.UtcNow.AddDays(5),
            Notes = vehicle.AuctionListing?.Notes
        };

        ViewData["Vehicle"] = vehicle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(PublishAuctionViewModel model)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.AuctionListing)
            .FirstOrDefaultAsync(v => v.Id == model.VehicleId);

        if (vehicle is null)
        {
            return NotFound();
        }

        if (model.EndsAt <= DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError(nameof(model.EndsAt), "Sluttid måste vara i framtiden.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Vehicle"] = vehicle;
            return View(model);
        }

        var listing = vehicle.AuctionListing ?? new AuctionListing { VehicleId = vehicle.Id };
        listing.ReservePrice = model.ReservePrice;
        listing.PublishedAt = DateTimeOffset.UtcNow;
        listing.EndsAt = model.EndsAt;
        listing.Notes = model.Notes?.Trim();
        listing.IsClosed = false;
        listing.SoldPrice = null;
        listing.WinnerDealerUserId = null;

        if (vehicle.AuctionListing is null)
        {
            vehicle.AuctionListing = listing;
        }

        vehicle.Status = VehicleStatus.InAuction;

        await context.SaveChangesAsync();
        TempData["Success"] = "Fordonet publicerades på auktionssidan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAuction(int id)
    {
        var listing = await context.AuctionListings
            .Include(a => a.Vehicle)
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (listing is null)
        {
            return NotFound();
        }

        var winningBid = listing.Bids
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.CreatedAt)
            .FirstOrDefault();

        listing.IsClosed = true;

        if (winningBid is not null && winningBid.Amount >= listing.ReservePrice)
        {
            listing.SoldPrice = winningBid.Amount;
            listing.WinnerDealerUserId = winningBid.DealerUserId;
            listing.Vehicle.Status = VehicleStatus.Sold;
            TempData["Success"] = $"Auktionen stängdes. Vinnande bud: {winningBid.Amount:C0}.";
        }
        else
        {
            listing.Vehicle.Status = VehicleStatus.ReadyForSale;
            listing.SoldPrice = null;
            listing.WinnerDealerUserId = null;
            TempData["Error"] = "Auktionen stängdes utan godkänt bud. Fordonet är tillbaka i sälj-kön.";
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
