using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Models;

namespace TelemetryApi.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/purge-and-reseed", async (TelemetryDbContext db, IWebHostEnvironment env) =>
        {
            var seedPath = Path.Combine(env.ContentRootPath, "..", "seed.json");
            if (!File.Exists(seedPath))
                seedPath = Path.Combine(env.ContentRootPath, "seed.json");
            if (!File.Exists(seedPath))
                return Results.NotFound(new { error = "seed.json not found" });

            db.TelemetryEvents.RemoveRange(db.TelemetryEvents);
            db.Devices.RemoveRange(db.Devices);
            db.Customers.RemoveRange(db.Customers);
            await db.SaveChangesAsync();

            var json = await File.ReadAllTextAsync(seedPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seed = JsonSerializer.Deserialize<SeedData>(json, options);
            if (seed is null)
                return Results.BadRequest(new { error = "Invalid seed.json" });

            foreach (var id in seed.Customers ?? [])
                db.Customers.Add(new Customer { CustomerId = id });
            foreach (var d in seed.Devices ?? [])
                db.Devices.Add(new Device { CustomerId = d.CustomerId, DeviceId = d.DeviceId, Label = d.Label, Location = d.Location });
            var telemetryDistinct = (seed.Telemetry ?? []).DistinctBy(x => x.EventId).ToList();
            foreach (var e in telemetryDistinct)
                db.TelemetryEvents.Add(new TelemetryEvent
                {
                    EventId = e.EventId,
                    CustomerId = e.CustomerId,
                    DeviceId = e.DeviceId,
                    RecordedAt = DateTimeOffset.Parse(e.RecordedAt).ToUnixTimeMilliseconds(),
                    Value = e.Value
                });
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Database purged and reseeded",
                customers = seed.Customers?.Count ?? 0,
                devices = seed.Devices?.Count ?? 0,
                events = telemetryDistinct.Count
            });
        });

        return app;
    }
}
