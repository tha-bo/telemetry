using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Models;

namespace TelemetryApi.Endpoints;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/devices", async (string? customerId, TelemetryDbContext db) =>
        {
            var query = db.Devices.AsNoTracking();
            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(d => d.CustomerId == customerId);
            var list = await query.ToListAsync();
            return list.Count > 0 ? Results.Ok(list) : Results.NotFound();
        });

        app.MapPost("/devices", async (CreateDeviceRequest request, TelemetryDbContext db) =>
        {
            var customerId = request.CustomerId?.Trim();
            var deviceId = request.DeviceId?.Trim();
            if (string.IsNullOrEmpty(customerId))
                return Results.BadRequest(new { error = "CustomerId is required." });
            if (string.IsNullOrEmpty(deviceId))
                return Results.BadRequest(new { error = "DeviceId is required." });

            var customerExists = await db.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return Results.NotFound(new { error = "Customer not found. Create the customer first." });

            var exists = await db.Devices.AnyAsync(d => d.CustomerId == customerId && d.DeviceId == deviceId);
            if (exists)
                return Results.Conflict(new { error = "A device with this CustomerId and DeviceId already exists." });

            var device = new Device
            {
                CustomerId = customerId,
                DeviceId = deviceId,
                Label = request.Label?.Trim() ?? string.Empty,
                Location = request.Location?.Trim() ?? string.Empty
            };
            db.Devices.Add(device);
            await db.SaveChangesAsync();
            return Results.Created($"/devices?customerId={Uri.EscapeDataString(customerId)}", device);
        });

        return app;
    }
}

internal record CreateDeviceRequest(string? CustomerId, string? DeviceId, string? Label, string? Location);
