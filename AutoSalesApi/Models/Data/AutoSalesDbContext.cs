using AutoSalesApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Data;

public class AutoSalesDbContext : DbContext
{
    public AutoSalesDbContext(DbContextOptions<AutoSalesDbContext> options) : base(options)
    {
    }

    public DbSet<Producer> Producers => Set<Producer>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite does not support decimal natively, so store money as REAL.
        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.PriceId);

            entity.Property(e => e.Price).HasConversion<double>();
            entity.Property(e => e.PrepCost).HasConversion<double>();
            entity.Property(e => e.TransportCost).HasConversion<double>();
            entity.Ignore(e => e.TotalCost);

            entity.HasOne(e => e.Model)
                .WithOne(m => m.PriceList)
                .HasForeignKey<PriceList>(e => e.ModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.TotalCost).HasConversion<double>();

            entity.HasOne(e => e.Client)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Model)
                .WithMany(m => m.Orders)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasOne(e => e.Producer)
                .WithMany(p => p.Offers)
                .HasForeignKey(e => e.ProducerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Model)
                .WithMany(m => m.Offers)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
