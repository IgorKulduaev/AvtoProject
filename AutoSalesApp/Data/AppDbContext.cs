using Microsoft.EntityFrameworkCore;
using AutoSalesApp.Models;

namespace AutoSalesApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Producer> Producers => Set<Producer>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=autosales.db");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Order>().ToTable("Order").HasKey(o => o.OrderId);
        mb.Entity<Model>().ToTable("Model").HasKey(m => m.ModelId);
        mb.Entity<Client>().ToTable("Client").HasKey(c => c.ClientId);
        mb.Entity<Producer>().ToTable("Producer").HasKey(p => p.ProducerId);
        mb.Entity<PriceList>().ToTable("PriceList").HasKey(p => p.PriceId);
        mb.Entity<Offer>().ToTable("Offer").HasKey(o => o.OfferId);

        mb.Entity<Offer>()
            .HasOne(o => o.Model).WithMany(m => m.Offers).HasForeignKey(o => o.ModelId);
        mb.Entity<Offer>()
            .HasOne(o => o.Producer).WithMany(p => p.Offers).HasForeignKey(o => o.ProducerId);

        mb.Entity<Order>()
            .HasOne(o => o.Model).WithMany(m => m.Orders).HasForeignKey(o => o.ModelId);
        mb.Entity<Order>()
            .HasOne(o => o.Client).WithMany(c => c.Orders).HasForeignKey(o => o.ClientId);

        mb.Entity<PriceList>()
            .HasOne(p => p.Model).WithOne(m => m.PriceList)
            .HasForeignKey<PriceList>(p => p.ModelId);

        mb.Entity<PriceList>()
            .HasIndex(p => p.ModelId)
            .IsUnique();
    }
}
