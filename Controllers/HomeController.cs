using System.Diagnostics;
using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new HomeDashboardViewModel
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        if (!model.IsAuthenticated)
        {
            return View(model);
        }

        model.TotalVehicles = await context.Vehicles.CountAsync();
        model.AwaitingInspection = await context.Vehicles.CountAsync(v =>
            v.Status == VehicleStatus.Registered || v.Status == VehicleStatus.InspectionInProgress);
        model.AwaitingFinance = await context.Vehicles.CountAsync(v =>
            v.Status == VehicleStatus.InspectionCompleted || v.Status == VehicleStatus.FinanceReview || v.Status == VehicleStatus.AwaitingSupplierDecision);
        model.ReadyForSale = await context.Vehicles.CountAsync(v => v.Status == VehicleStatus.ReadyForSale);
        var now = DateTimeOffset.UtcNow;
        var openAuctionEndDates = await context.AuctionListings
            .Where(a => !a.IsClosed)
            .Select(a => a.EndsAt)
            .ToListAsync();
        model.ActiveAuctions = openAuctionEndDates.Count(endsAt => endsAt > now);

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
