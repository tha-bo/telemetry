using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Models;

namespace TelemetryApi.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/customers", async (TelemetryDbContext db) =>
        {
            var list = await db.Customers.Select(c => c.CustomerId).ToListAsync();
            return list.Count > 0 ? Results.Ok(list) : Results.NotFound();
        });

        app.MapPost("/customers", async (CreateCustomerRequest request, TelemetryDbContext db) =>
        {
            var customerId = request.CustomerId?.Trim();
            if (string.IsNullOrEmpty(customerId))
                return Results.BadRequest(new { error = "CustomerId is required." });

            var exists = await db.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (exists)
                return Results.Conflict(new { error = "A customer with this CustomerId already exists." });

            db.Customers.Add(new Customer { CustomerId = customerId });
            await db.SaveChangesAsync();
            return Results.Created($"/customers/{customerId}", new { customerId });
        });

        return app;
    }
}

internal record CreateCustomerRequest(string? CustomerId);
