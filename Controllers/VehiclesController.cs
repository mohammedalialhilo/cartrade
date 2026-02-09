using Cartrade.Data;
using Cartrade.Models;
using Cartrade.Models.Enums;
using Cartrade.Models.ViewModels;
using Cartrade.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize(Policy = "InternalOnly")]
public class VehiclesController(
    ApplicationDbContext context,
    CsvVehicleImporter importer,
    UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? status, string? search)
    {
        var query = context.Vehicles.AsNoTracking();

        if (Enum.TryParse<VehicleStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(v => v.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.RegistrationNumber.ToLower().Contains(term) ||
                v.Make.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term) ||
                (v.PartnerName != null && v.PartnerName.ToLower().Contains(term)));
        }

        ViewData["SelectedStatus"] = status;
        ViewData["Search"] = search;
        ViewData["Statuses"] = Enum.GetValues<VehicleStatus>();

        var vehicles = await query.ToListAsync();
        vehicles = vehicles
            .OrderByDescending(v => v.CreatedAt)
            .Take(500)
            .ToList();

        return View(vehicles);
    }

    public async Task<IActionResult> Details(int id)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.InspectionReport)
            .Include(v => v.FinanceEvaluation)
            .Include(v => v.AuctionListing)
                .ThenInclude(l => l!.Bids)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    public IActionResult Create()
    {
        return View(new ManualVehicleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManualVehicleViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var normalizedReg = input.RegistrationNumber.Trim().ToUpperInvariant();
        var exists = await context.Vehicles.AnyAsync(v => v.RegistrationNumber == normalizedReg);
        if (exists)
        {
            ModelState.AddModelError(nameof(input.RegistrationNumber), "Fordon med detta registreringsnummer finns redan.");
            return View(input);
        }

        var user = await userManager.GetUserAsync(User);

        var vehicle = new Vehicle
        {
            ExternalReference = input.ExternalReference?.Trim(),
            RegistrationNumber = normalizedReg,
            Vin = input.Vin?.Trim(),
            Make = input.Make.Trim(),
            Model = input.Model.Trim(),
            ModelYear = input.ModelYear,
            OdometerKm = input.OdometerKm,
            Color = input.Color?.Trim(),
            FuelType = input.FuelType?.Trim(),
            PartnerName = input.PartnerName?.Trim(),
            Source = VehicleSource.ManualEntry,
            Status = VehicleStatus.Registered,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = user?.Id
        };

        await context.Vehicles.AddAsync(vehicle);
        await context.SaveChangesAsync();

        TempData["Success"] = "Fordonet registrerades.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Import()
    {
        return View(new VehicleImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    public async Task<IActionResult> Import(VehicleImportViewModel model, CancellationToken cancellationToken)
    {
        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Välj en fil att importera.");
            return View(model);
        }

        await using var stream = model.File.OpenReadStream();
        var user = await userManager.GetUserAsync(User);
        var parseResult = await importer.ParseAsync(stream, user?.Id, cancellationToken);

        if (parseResult.Vehicles.Count == 0)
        {
            model.Warnings = parseResult.Warnings;
            return View(model);
        }

        var incomingRegs = parseResult.Vehicles.Select(v => v.RegistrationNumber).ToList();
        var existingRegs = await context.Vehicles
            .Where(v => incomingRegs.Contains(v.RegistrationNumber))
            .Select(v => v.RegistrationNumber)
            .ToListAsync(cancellationToken);

        var existingSet = existingRegs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toAdd = parseResult.Vehicles
            .Where(v => !existingSet.Contains(v.RegistrationNumber))
            .ToList();

        model.ImportedCount = toAdd.Count;
        model.SkippedCount = parseResult.Vehicles.Count - toAdd.Count;
        model.Warnings = parseResult.Warnings;

        foreach (var duplicateReg in parseResult.Vehicles
                     .Where(v => existingSet.Contains(v.RegistrationNumber))
                     .Select(v => v.RegistrationNumber)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            model.Warnings.Add($"Registreringsnummer {duplicateReg} finns redan och hoppades över.");
        }

        if (toAdd.Count > 0)
        {
            await context.Vehicles.AddRangeAsync(toAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return View(model);
    }
}
