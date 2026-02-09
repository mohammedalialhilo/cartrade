using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize(Roles = $"{SeedData.AdminRole},{SeedData.InspectionRole}")]
public class InspectionController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vehicles = await context.Vehicles
            .Include(v => v.InspectionReport)
            .Where(v => v.Status == VehicleStatus.Registered ||
                        v.Status == VehicleStatus.InspectionInProgress ||
                        v.Status == VehicleStatus.InspectionCompleted)
            .ToListAsync();

        vehicles = vehicles
            .OrderBy(v => v.Status)
            .ThenByDescending(v => v.CreatedAt)
            .ToList();

        return View(vehicles);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.InspectionReport)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null)
        {
            return NotFound();
        }

        var report = vehicle.InspectionReport;

        var model = new InspectionUpdateViewModel
        {
            VehicleId = vehicle.Id,
            StartedAt = report?.StartedAt,
            CompletedAt = report?.CompletedAt,
            Comments = report?.Comments,
            DamageNotes = report?.DamageNotes,
            EstimatedRepairCost = report?.EstimatedRepairCost ?? 0
        };

        ViewData["Vehicle"] = vehicle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InspectionUpdateViewModel model)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.InspectionReport)
            .FirstOrDefaultAsync(v => v.Id == model.VehicleId);

        if (vehicle is null)
        {
            return NotFound();
        }

        if (model.CompletedAt.HasValue && model.StartedAt.HasValue && model.CompletedAt < model.StartedAt)
        {
            ModelState.AddModelError(nameof(model.CompletedAt), "Slutdatum kan inte vara före startdatum.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Vehicle"] = vehicle;
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);

        var report = vehicle.InspectionReport ?? new InspectionReport { VehicleId = vehicle.Id };
        report.StartedAt = model.StartedAt;
        report.CompletedAt = model.CompletedAt;
        report.Comments = model.Comments?.Trim();
        report.DamageNotes = model.DamageNotes?.Trim();
        report.EstimatedRepairCost = model.EstimatedRepairCost;
        report.InspectorUserId = user?.Id;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        if (vehicle.InspectionReport is null)
        {
            vehicle.InspectionReport = report;
        }

        if (model.CompletedAt.HasValue)
        {
            vehicle.Status = VehicleStatus.InspectionCompleted;
        }
        else if (model.StartedAt.HasValue)
        {
            vehicle.Status = VehicleStatus.InspectionInProgress;
        }
        else
        {
            vehicle.Status = VehicleStatus.Registered;
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Besiktningsuppgifter sparades.";
        return RedirectToAction(nameof(Index));
    }
}
