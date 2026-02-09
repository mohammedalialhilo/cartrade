using Cartrade.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<FinanceEvaluation> FinanceEvaluations => Set<FinanceEvaluation>();
    public DbSet<AuctionListing> AuctionListings => Set<AuctionListing>();
    public DbSet<AuctionBid> AuctionBids => Set<AuctionBid>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>()
            .HasIndex(v => v.RegistrationNumber)
            .IsUnique();

        builder.Entity<Vehicle>()
            .HasOne(v => v.InspectionReport)
            .WithOne(i => i.Vehicle)
            .HasForeignKey<InspectionReport>(i => i.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vehicle>()
            .HasOne(v => v.FinanceEvaluation)
            .WithOne(f => f.Vehicle)
            .HasForeignKey<FinanceEvaluation>(f => f.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vehicle>()
            .HasOne(v => v.AuctionListing)
            .WithOne(a => a.Vehicle)
            .HasForeignKey<AuctionListing>(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AuctionBid>()
            .HasOne(b => b.AuctionListing)
            .WithMany(l => l.Bids)
            .HasForeignKey(b => b.AuctionListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AuctionBid>()
            .HasIndex(b => new { b.AuctionListingId, b.Amount });
    }
}
