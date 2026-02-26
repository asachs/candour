namespace Candour.Core.ValueObjects;

public class AggregateData
{
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public int TotalResponses { get; set; }
    public List<QuestionAggregate> Questions { get; set; } = new();
}

public class QuestionAggregate
{
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public Dictionary<string, int> OptionCounts { get; set; } = new();
    public Dictionary<string, double> OptionPercentages { get; set; } = new();
    public List<string> FreeTextAnswers { get; set; } = new(); // Shuffled, no timestamps
    public double? AverageRating { get; set; }
}
