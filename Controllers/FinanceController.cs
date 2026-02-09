using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize(Roles = $"{SeedData.AdminRole},{SeedData.FinanceRole}")]
public class FinanceController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vehicles = await context.Vehicles
            .Include(v => v.InspectionReport)
            .Include(v => v.FinanceEvaluation)
            .Where(v => v.Status == VehicleStatus.InspectionCompleted ||
                        v.Status == VehicleStatus.FinanceReview ||
                        v.Status == VehicleStatus.AwaitingSupplierDecision ||
                        v.Status == VehicleStatus.ReadyForSale ||
                        v.Status == VehicleStatus.Rejected)
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
            .Include(v => v.FinanceEvaluation)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null)
        {
            return NotFound();
        }

        var evaluation = vehicle.FinanceEvaluation;
        var model = new FinanceUpdateViewModel
        {
            VehicleId = vehicle.Id,
            ConditionGrade = evaluation?.ConditionGrade,
            InternalValuation = evaluation?.InternalValuation ?? 0,
            OfferPrice = evaluation?.OfferPrice ?? 0,
            SupplierAccepted = evaluation?.SupplierAccepted,
            Notes = evaluation?.Notes
        };

        ViewData["Vehicle"] = vehicle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FinanceUpdateViewModel model)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.FinanceEvaluation)
            .FirstOrDefaultAsync(v => v.Id == model.VehicleId);

        if (vehicle is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["Vehicle"] = vehicle;
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        var evaluation = vehicle.FinanceEvaluation ?? new FinanceEvaluation { VehicleId = vehicle.Id };
        evaluation.ConditionGrade = model.ConditionGrade?.Trim().ToUpperInvariant();
        evaluation.InternalValuation = model.InternalValuation;
        evaluation.OfferPrice = model.OfferPrice;
        evaluation.SupplierAccepted = model.SupplierAccepted;
        evaluation.DecisionDate = model.SupplierAccepted.HasValue ? DateTimeOffset.UtcNow : null;
        evaluation.Notes = model.Notes?.Trim();
        evaluation.FinanceUserId = user?.Id;
        evaluation.UpdatedAt = DateTimeOffset.UtcNow;

        if (vehicle.FinanceEvaluation is null)
        {
            vehicle.FinanceEvaluation = evaluation;
        }

        if (model.SupplierAccepted == true)
        {
            vehicle.Status = VehicleStatus.ReadyForSale;
        }
        else if (model.SupplierAccepted == false)
        {
            vehicle.Status = VehicleStatus.Rejected;
        }
        else if (model.OfferPrice > 0)
        {
            vehicle.Status = VehicleStatus.AwaitingSupplierDecision;
        }
        else
        {
            vehicle.Status = VehicleStatus.FinanceReview;
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Finansbeslut sparades.";
        return RedirectToAction(nameof(Index));
    }
}
