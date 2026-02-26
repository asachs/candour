namespace Candour.Shared.Contracts;

public class CreateSurveyRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AnonymityThreshold { get; set; } = 5;
    public int TimestampJitterMinutes { get; set; } = 10;
    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public bool Required { get; set; } = true;
    public int Order { get; set; }
}
