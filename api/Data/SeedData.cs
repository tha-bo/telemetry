namespace TelemetryApi.Data;

/// <summary>DTO for deserializing seed.json.</summary>
public record SeedData(
    List<string>? Customers,
    List<DeviceSeed>? Devices,
    List<TelemetryEventSeed>? Telemetry);

public record DeviceSeed(string CustomerId, string DeviceId, string Label, string Location);

public record TelemetryEventSeed(string CustomerId, string DeviceId, string EventId, string RecordedAt, double Value);
