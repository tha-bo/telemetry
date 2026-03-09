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
            if (list.Count == 0)
                return Results.NotFound();

            var result = list
                .Select(e => new TelemetryEventResponse(
                    e.EventId,
                    e.CustomerId,
                    e.DeviceId,
                    e.RecordedAt,
                    e.Value))
                .ToList();

            return Results.Ok(result);
        });

        app.MapGet("/customers/{customerId}/telemetry", async (string customerId, string? deviceId, long? from, long? to, TelemetryDbContext db) =>
        {
            var customerExists = await db.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return Results.NotFound(new { error = "Customer not found." });

            var query = db.TelemetryEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId);

            if (!string.IsNullOrEmpty(deviceId))
                query = query.Where(e => e.DeviceId == deviceId);

            if (from.HasValue)
                query = query.Where(e => e.RecordedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.RecordedAt <= to.Value);

            var list = await query.OrderBy(e => e.RecordedAt).ToListAsync();
            var result = list
                .Select(e => new TelemetryEventResponse(
                    e.EventId,
                    e.CustomerId,
                    e.DeviceId,
                    e.RecordedAt,
                    e.Value))
                .ToList();

            return Results.Ok(result);
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

            var recordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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

            var response = new TelemetryEventResponse(
                evt.EventId,
                evt.CustomerId,
                evt.DeviceId,
                evt.RecordedAt,
                evt.Value);

            return Results.Created(
                $"/telemetry/events?customerId={Uri.EscapeDataString(customerId)}&deviceId={Uri.EscapeDataString(deviceId)}",
                response);
        });

        app.MapGet("/telemetry/info", () => Results.Ok(new { Unit = "C", Metric = "temperature" }));

        return app;
    }
}

internal record TelemetryEventResponse(string EventId, string CustomerId, string DeviceId, long RecordedAt, double Value);

internal record CreateTelemetryEventRequest(string? CustomerId, string? DeviceId, string? EventId, long? RecordedAt, double Value);
