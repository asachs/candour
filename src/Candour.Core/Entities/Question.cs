namespace Candour.Core.Entities;

using Candour.Core.Enums;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyId { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Options { get; set; } = "[]"; // JSON array
    public bool Required { get; set; } = true;
    public int Order { get; set; }
    public string Settings { get; set; } = "{}"; // JSON

    public Survey? Survey { get; set; }
}
