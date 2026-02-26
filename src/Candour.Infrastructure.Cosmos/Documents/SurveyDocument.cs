namespace Candour.Infrastructure.Cosmos.Documents;

using System.Text.Json;
using System.Text.Json.Serialization;
using Candour.Core.Entities;
using Candour.Core.Enums;

public class SurveyDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("creatorId")]
    public string CreatorId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("settings")]
    public string Settings { get; set; } = "{}";

    [JsonPropertyName("anonymityThreshold")]
    public int AnonymityThreshold { get; set; }

    [JsonPropertyName("timestampJitterMinutes")]
    public int TimestampJitterMinutes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("batchSecret")]
    public string BatchSecret { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<QuestionDocument> Questions { get; set; } = new();

    public static SurveyDocument FromEntity(Survey entity) => new()
    {
        Id = entity.Id.ToString(),
        CreatorId = entity.CreatorId,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status.ToString(),
        Settings = entity.Settings,
        AnonymityThreshold = entity.AnonymityThreshold,
        TimestampJitterMinutes = entity.TimestampJitterMinutes,
        CreatedAt = entity.CreatedAt,
        BatchSecret = entity.BatchSecret,
        Questions = entity.Questions.Select(QuestionDocument.FromEntity).ToList()
    };

    public Survey ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        CreatorId = CreatorId,
        Title = Title,
        Description = Description,
        Status = Enum.Parse<SurveyStatus>(Status),
        Settings = Settings,
        AnonymityThreshold = AnonymityThreshold,
        TimestampJitterMinutes = TimestampJitterMinutes,
        CreatedAt = CreatedAt,
        BatchSecret = BatchSecret,
        Questions = Questions.Select(q => q.ToEntity(Guid.Parse(Id))).ToList()
    };
}

public class QuestionDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public string Options { get; set; } = "[]";

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("settings")]
    public string Settings { get; set; } = "{}";

    public static QuestionDocument FromEntity(Question entity) => new()
    {
        Id = entity.Id.ToString(),
        Type = entity.Type.ToString(),
        Text = entity.Text,
        Options = entity.Options,
        Required = entity.Required,
        Order = entity.Order,
        Settings = entity.Settings
    };

    public Question ToEntity(Guid surveyId) => new()
    {
        Id = Guid.Parse(Id),
        SurveyId = surveyId,
        Type = Enum.Parse<QuestionType>(Type),
        Text = Text,
        Options = Options,
        Required = Required,
        Order = Order,
        Settings = Settings
    };
}
