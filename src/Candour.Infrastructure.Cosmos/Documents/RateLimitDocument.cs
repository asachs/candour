namespace Candour.Infrastructure.Cosmos.Documents;

using System.Text.Json.Serialization;

public class RateLimitDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("windowStart")]
    public DateTime WindowStart { get; set; }

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
}
