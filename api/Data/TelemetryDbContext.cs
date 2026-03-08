using Microsoft.EntityFrameworkCore;
using TelemetryApi.Models;

namespace TelemetryApi.Data;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasKey(c => c.CustomerId);

        modelBuilder.Entity<Device>()
            .HasKey(d => new { d.CustomerId, d.DeviceId });

        modelBuilder.Entity<TelemetryEvent>()
            .HasKey(e => e.EventId);
    }
}
