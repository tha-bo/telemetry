using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Endpoints;
using TelemetryApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TelemetryDb")));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Telemetry API", Version = "v1" });
});

var app = builder.Build();

app.UseCors();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    db.Database.EnsureCreated();

    if (!db.Customers.Any())
    {
        var seedPath = Path.Combine(app.Environment.ContentRootPath, "..", "seed.json");
        if (!File.Exists(seedPath))
            seedPath = Path.Combine(app.Environment.ContentRootPath, "seed.json");

        if (File.Exists(seedPath))
        {
            var json = await File.ReadAllTextAsync(seedPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seed = JsonSerializer.Deserialize<SeedData>(json, options);
            if (seed is not null)
            {
                foreach (var id in seed.Customers ?? [])
                    db.Customers.Add(new Customer { CustomerId = id });

                foreach (var d in seed.Devices ?? [])
                    db.Devices.Add(new Device
                    {
                        CustomerId = d.CustomerId,
                        DeviceId = d.DeviceId,
                        Label = d.Label,
                        Location = d.Location
                    });

                if (seed.Telemetry is { Count: > 0 })
                {
                    foreach (var e in seed.Telemetry.DistinctBy(x => x.EventId))
                        db.TelemetryEvents.Add(new TelemetryEvent
                        {
                            EventId = e.EventId,
                            CustomerId = e.CustomerId,
                            DeviceId = e.DeviceId,
                            RecordedAt = DateTimeOffset.Parse(e.RecordedAt).ToUnixTimeMilliseconds(),
                            Value = e.Value
                        });
                }

                await db.SaveChangesAsync();
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapCustomerEndpoints();
app.MapDeviceEndpoints();
app.MapTelemetryEndpoints();
app.MapAdminEndpoints();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
