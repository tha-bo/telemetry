using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Models;

namespace TelemetryApi.Endpoints;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/telemetry/events", async (string? customerId, string? deviceId, TelemetryDbContext db) =>
        {
            var query = db.TelemetryEvents.AsNoTracking();
            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(e => e.CustomerId == customerId);
            if (!string.IsNullOrEmpty(deviceId))
                query = query.Where(e => e.DeviceId == deviceId);
            var list = await query.OrderBy(e => e.RecordedAt).ToListAsync();
            return list.Count > 0 ? Results.Ok(list) : Results.NotFound();
        });

        app.MapGet("/customers/{customerId}/telemetry", async (string customerId, string? deviceId, string? from, string? to, TelemetryDbContext db) =>
        {
            var customerExists = await db.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return Results.NotFound(new { error = "Customer not found." });

            var query = db.TelemetryEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId);

            if (!string.IsNullOrEmpty(deviceId))
                query = query.Where(e => e.DeviceId == deviceId);

            if (!string.IsNullOrEmpty(from) && DateTimeOffset.TryParse(from, out var fromDate))
                query = query.Where(e => e.RecordedAt >= fromDate);

            if (!string.IsNullOrEmpty(to) && DateTimeOffset.TryParse(to, out var toDate))
                query = query.Where(e => e.RecordedAt <= toDate);

            var list = await query.OrderBy(e => e.RecordedAt).ToListAsync();
            return Results.Ok(list);
        });

        app.MapPost("/telemetry/events", async (CreateTelemetryEventRequest request, TelemetryDbContext db) =>
        {
            var customerId = request.CustomerId?.Trim();
            var deviceId = request.DeviceId?.Trim();
            var eventId = request.EventId?.Trim();
            if (string.IsNullOrEmpty(customerId))
                return Results.BadRequest(new { error = "CustomerId is required." });
            if (string.IsNullOrEmpty(deviceId))
                return Results.BadRequest(new { error = "DeviceId is required." });
            if (string.IsNullOrEmpty(eventId))
                return Results.BadRequest(new { error = "EventId is required." });

            var customerExists = await db.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return Results.NotFound(new { error = "Customer not found." });

            var deviceExists = await db.Devices.AnyAsync(d => d.CustomerId == customerId && d.DeviceId == deviceId);
            if (!deviceExists)
                return Results.NotFound(new { error = "Device not found for this customer." });

            var eventExists = await db.TelemetryEvents.AnyAsync(e => e.EventId == eventId);
            if (eventExists)
                return Results.Conflict(new { error = "An event with this EventId already exists." });

            if (!DateTimeOffset.TryParse(request.RecordedAt, out var recordedAt))
                recordedAt = DateTimeOffset.UtcNow;

            var evt = new TelemetryEvent
            {
                EventId = eventId,
                CustomerId = customerId,
                DeviceId = deviceId,
                RecordedAt = recordedAt,
                Value = request.Value
            };
            db.TelemetryEvents.Add(evt);
            await db.SaveChangesAsync();
            return Results.Created($"/telemetry/events?customerId={Uri.EscapeDataString(customerId)}&deviceId={Uri.EscapeDataString(deviceId)}", evt);
        });

        app.MapGet("/telemetry/info", () => Results.Ok(new { Unit = "C", Metric = "temperature" }));

        return app;
    }
}

internal record CreateTelemetryEventRequest(string? CustomerId, string? DeviceId, string? EventId, string? RecordedAt, double Value);
