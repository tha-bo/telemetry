namespace TelemetryApi.Models;

public class Device
{
    public string CustomerId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
