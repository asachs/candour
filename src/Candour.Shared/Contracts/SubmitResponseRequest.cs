namespace Candour.Shared.Contracts;

public class SubmitResponseRequest
{
    public string Token { get; set; } = string.Empty;
    public Dictionary<string, string> Answers { get; set; } = new(); // QuestionId -> Answer
}
