namespace Candour.Core.Entities;

public class RateLimitEntry
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime WindowStart { get; set; }
    public int TtlSeconds { get; set; }
}
