namespace Candour.Infrastructure.Cosmos.Documents;

using System.Text.Json.Serialization;
using Candour.Core.Entities;

public class ResponseDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonPropertyName("answers")]
    public string Answers { get; set; } = "{}";

    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    public static ResponseDocument FromEntity(SurveyResponse entity) => new()
    {
        Id = entity.Id.ToString(),
        SurveyId = entity.SurveyId.ToString(),
        Answers = entity.Answers,
        SubmittedAt = entity.SubmittedAt
    };

    public SurveyResponse ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        SurveyId = Guid.Parse(SurveyId),
        Answers = Answers,
        SubmittedAt = SubmittedAt
    };
}
