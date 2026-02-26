namespace Candour.Core.ValueObjects;

public class AnalysisReport
{
    public Guid SurveyId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Themes { get; set; } = new();
    public List<string> KeyInsights { get; set; } = new();
    public string SentimentOverview { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
