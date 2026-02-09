using Cartrade.Models;
using Cartrade.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Data;

public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string InspectionRole = "Inspection";
    public const string FinanceRole = "Finance";
    public const string SalesRole = "Sales";
    public const string DealerRole = "Dealer";

    private static readonly (string Email, string Password, string Role)[] DefaultUsers =
    [
        ("admin@cartrade.local", "CarTrade!2026", AdminRole),
        ("inspection@cartrade.local", "CarTrade!2026", InspectionRole),
        ("finance@cartrade.local", "CarTrade!2026", FinanceRole),
        ("sales@cartrade.local", "CarTrade!2026", SalesRole),
        ("dealer@cartrade.local", "CarTrade!2026", DealerRole)
    ];

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await EnsureRoles(roleManager);
        await EnsureUsers(userManager);
        await EnsureSampleVehicles(context, userManager);
    }

    private static async Task EnsureRoles(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { AdminRole, InspectionRole, FinanceRole, SalesRole, DealerRole };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task EnsureUsers(UserManager<IdentityUser> userManager)
    {
        foreach (var (email, password, role) in DefaultUsers)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            if (role == AdminRole)
            {
                foreach (var elevatedRole in new[] { InspectionRole, FinanceRole, SalesRole, DealerRole })
                {
                    if (!await userManager.IsInRoleAsync(user, elevatedRole))
                    {
                        await userManager.AddToRoleAsync(user, elevatedRole);
                    }
                }
            }
        }
    }

    private static async Task EnsureSampleVehicles(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        var existingRegistrations = (await context.Vehicles
            .Select(v => v.RegistrationNumber)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var adminId = (await userManager.FindByEmailAsync("admin@cartrade.local"))?.Id;
        var inspectorId = (await userManager.FindByEmailAsync("inspection@cartrade.local"))?.Id;
        var financeId = (await userManager.FindByEmailAsync("finance@cartrade.local"))?.Id;
        var dealerId = (await userManager.FindByEmailAsync("dealer@cartrade.local"))?.Id ?? "dealer-demo";

        var now = DateTimeOffset.UtcNow;

        var vehicles = new List<Vehicle>
        {
            new()
            {
                ExternalReference = "LEASE-DUM-101",
                RegistrationNumber = "DUM101",
                Vin = "WVWZZZ3CZEE110101",
                Make = "Volkswagen",
                Model = "Golf",
                ModelYear = 2021,
                OdometerKm = 64000,
                Color = "Black",
                FuelType = "Diesel",
                PartnerName = "VW Leasing",
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.Registered,
                CreatedAt = now.AddDays(-12),
                CreatedByUserId = adminId
            },
            new()
            {
                ExternalReference = "LEASE-DUM-102",
                RegistrationNumber = "DUM102",
                Vin = "YV1ZZZ10203040506",
                Make = "Volvo",
                Model = "XC40",
                ModelYear = 2022,
                OdometerKm = 41000,
                Color = "White",
                FuelType = "Hybrid",
                PartnerName = "Volvo Finans",
                Source = VehicleSource.ManualEntry,
                Status = VehicleStatus.InspectionInProgress,
                CreatedAt = now.AddDays(-10),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-2),
                    Comments = "Inspection started, bumper scratch noted.",
                    DamageNotes = "Front bumper scratch",
                    EstimatedRepairCost = 3500,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-2)
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-103",
                RegistrationNumber = "DUM103",
                Vin = "WBAZZZ10303040506",
                Make = "BMW",
                Model = "320d",
                ModelYear = 2020,
                OdometerKm = 72000,
                Color = "Grey",
                FuelType = "Diesel",
                PartnerName = "BMW Financial Services",
                Source = VehicleSource.ManualEntry,
                Status = VehicleStatus.InspectionCompleted,
                CreatedAt = now.AddDays(-9),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-7),
                    CompletedAt = now.AddDays(-6),
                    Comments = "Vehicle is drivable and clean.",
                    DamageNotes = "Minor wheel scratches",
                    EstimatedRepairCost = 2400,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-6)
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-104",
                RegistrationNumber = "DUM104",
                Vin = "WDDZZZ10403040506",
                Make = "Mercedes",
                Model = "C300e",
                ModelYear = 2021,
                OdometerKm = 56000,
                Color = "Silver",
                FuelType = "Hybrid",
                PartnerName = "Mercedes Finance",
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.FinanceReview,
                CreatedAt = now.AddDays(-8),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-6),
                    CompletedAt = now.AddDays(-5),
                    Comments = "No mechanical issues found.",
                    DamageNotes = "Interior wear on driver seat",
                    EstimatedRepairCost = 1800,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-5)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "B",
                    InternalValuation = 228000,
                    OfferPrice = 0,
                    SupplierAccepted = null,
                    Notes = "Waiting for final offer calculation.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-4)
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-105",
                RegistrationNumber = "DUM105",
                Vin = "YT1ZZZ10503040506",
                Make = "Toyota",
                Model = "RAV4",
                ModelYear = 2022,
                OdometerKm = 35000,
                Color = "Blue",
                FuelType = "Hybrid",
                PartnerName = "Toyota Kreditbank",
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.AwaitingSupplierDecision,
                CreatedAt = now.AddDays(-7),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-5),
                    CompletedAt = now.AddDays(-4),
                    Comments = "Vehicle in good condition.",
                    DamageNotes = "Small rear door dent",
                    EstimatedRepairCost = 4200,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-4)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "B",
                    InternalValuation = 262000,
                    OfferPrice = 251000,
                    SupplierAccepted = null,
                    Notes = "Offer sent to supplier, waiting response.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-3)
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-106",
                RegistrationNumber = "DUM106",
                Vin = "5YJ3E1EA7LF010106",
                Make = "Tesla",
                Model = "Model 3",
                ModelYear = 2021,
                OdometerKm = 69000,
                Color = "Red",
                FuelType = "Electric",
                PartnerName = "Tesla Lease",
                Source = VehicleSource.ManualEntry,
                Status = VehicleStatus.ReadyForSale,
                CreatedAt = now.AddDays(-6),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-4),
                    CompletedAt = now.AddDays(-3),
                    Comments = "Battery health normal.",
                    DamageNotes = "Minor scratches on rear bumper",
                    EstimatedRepairCost = 2900,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-3)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "A",
                    InternalValuation = 278000,
                    OfferPrice = 269000,
                    SupplierAccepted = true,
                    DecisionDate = now.AddDays(-2),
                    Notes = "Supplier accepted the offer.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-2)
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-107",
                RegistrationNumber = "DUM107",
                Vin = "LPSZZZ10703040506",
                Make = "Polestar",
                Model = "2",
                ModelYear = 2022,
                OdometerKm = 43000,
                Color = "White",
                FuelType = "Electric",
                PartnerName = "Polestar Fleet",
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.InAuction,
                CreatedAt = now.AddDays(-5),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-3),
                    CompletedAt = now.AddDays(-2),
                    Comments = "Road test passed.",
                    DamageNotes = "None",
                    EstimatedRepairCost = 0,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-2)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "A",
                    InternalValuation = 284000,
                    OfferPrice = 276000,
                    SupplierAccepted = true,
                    DecisionDate = now.AddDays(-2),
                    Notes = "Approved for sale.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-2)
                },
                AuctionListing = new AuctionListing
                {
                    ReservePrice = 275000,
                    PublishedAt = now.AddDays(-1),
                    EndsAt = now.AddDays(2),
                    IsClosed = false,
                    Notes = "Live demo auction",
                    Bids =
                    [
                        new AuctionBid { DealerUserId = dealerId, Amount = 276500, CreatedAt = now.AddHours(-18) },
                        new AuctionBid { DealerUserId = dealerId, Amount = 279000, CreatedAt = now.AddHours(-4) }
                    ]
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-108",
                RegistrationNumber = "DUM108",
                Vin = "WBAZZZ10803040506",
                Make = "BMW",
                Model = "X1",
                ModelYear = 2020,
                OdometerKm = 78000,
                Color = "Blue",
                FuelType = "Petrol",
                PartnerName = "BMW Financial Services",
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.Sold,
                CreatedAt = now.AddDays(-11),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-9),
                    CompletedAt = now.AddDays(-8),
                    Comments = "Passed inspection.",
                    DamageNotes = "Paint chip on hood",
                    EstimatedRepairCost = 2200,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-8)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "B",
                    InternalValuation = 219000,
                    OfferPrice = 212000,
                    SupplierAccepted = true,
                    DecisionDate = now.AddDays(-7),
                    Notes = "Approved and listed.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-7)
                },
                AuctionListing = new AuctionListing
                {
                    ReservePrice = 210000,
                    PublishedAt = now.AddDays(-6),
                    EndsAt = now.AddDays(-2),
                    IsClosed = true,
                    WinnerDealerUserId = dealerId,
                    SoldPrice = 218500,
                    Notes = "Completed auction",
                    Bids =
                    [
                        new AuctionBid { DealerUserId = dealerId, Amount = 213000, CreatedAt = now.AddDays(-5) },
                        new AuctionBid { DealerUserId = dealerId, Amount = 218500, CreatedAt = now.AddDays(-2).AddHours(-1) }
                    ]
                }
            },
            new()
            {
                ExternalReference = "LEASE-DUM-109",
                RegistrationNumber = "DUM109",
                Vin = "YV1ZZZ10903040506",
                Make = "Volvo",
                Model = "V60",
                ModelYear = 2019,
                OdometerKm = 92000,
                Color = "Grey",
                FuelType = "Diesel",
                PartnerName = "Volvo Finans",
                Source = VehicleSource.ManualEntry,
                Status = VehicleStatus.Rejected,
                CreatedAt = now.AddDays(-13),
                CreatedByUserId = adminId,
                InspectionReport = new InspectionReport
                {
                    StartedAt = now.AddDays(-11),
                    CompletedAt = now.AddDays(-10),
                    Comments = "High wear and tear.",
                    DamageNotes = "Multiple dents and cracked mirror",
                    EstimatedRepairCost = 18000,
                    InspectorUserId = inspectorId,
                    UpdatedAt = now.AddDays(-10)
                },
                FinanceEvaluation = new FinanceEvaluation
                {
                    ConditionGrade = "D",
                    InternalValuation = 98000,
                    OfferPrice = 74000,
                    SupplierAccepted = false,
                    DecisionDate = now.AddDays(-9),
                    Notes = "Supplier declined offer.",
                    FinanceUserId = financeId,
                    UpdatedAt = now.AddDays(-9)
                }
            }
        };

        var toInsert = vehicles
            .Where(v => !existingRegistrations.Contains(v.RegistrationNumber))
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        await context.Vehicles.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }
}
