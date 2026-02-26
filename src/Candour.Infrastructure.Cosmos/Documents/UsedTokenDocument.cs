namespace Candour.Infrastructure.Cosmos.Documents;

using System.Text.Json.Serialization;

public class UsedTokenDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonPropertyName("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    public static UsedTokenDocument Create(string tokenHash, Guid surveyId) => new()
    {
        Id = $"{surveyId}:{tokenHash}",
        SurveyId = surveyId.ToString(),
        TokenHash = tokenHash
    };
}
