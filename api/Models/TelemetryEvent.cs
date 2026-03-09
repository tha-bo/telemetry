namespace TelemetryApi.Models;

public class TelemetryEvent
{
    public string EventId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public long RecordedAt { get; set; }
    public double Value { get; set; }
}
