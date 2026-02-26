namespace Candour.Core.Entities;

using Candour.Core.Enums;

public class Survey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    public string Settings { get; set; } = "{}"; // JSON
    public int AnonymityThreshold { get; set; } = 5;
    public int TimestampJitterMinutes { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string BatchSecret { get; set; } = string.Empty; // 256-bit key, base64

    public List<Question> Questions { get; set; } = new();
}
