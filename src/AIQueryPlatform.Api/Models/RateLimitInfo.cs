namespace AIQueryPlatform.Api.Models;

/// <summary>
/// Rate limiting tracker per tenant
/// </summary>
public class RateLimitInfo
{
    public Guid TenantId { get; set; }
    public int RequestsInLastMinute { get; set; }
    public int RequestsInLastHour { get; set; }
    public DateTime LastRequestTime { get; set; }
    public Queue<DateTime> MinuteWindow { get; set; } = new();
    public Queue<DateTime> HourWindow { get; set; } = new();
}
