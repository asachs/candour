namespace Candour.Shared.Models;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public bool Required { get; set; }
    public int Order { get; set; }
}
