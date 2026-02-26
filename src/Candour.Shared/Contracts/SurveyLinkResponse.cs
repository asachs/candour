namespace Candour.Shared.Contracts;

public class SurveyLinkResponse
{
    public Guid SurveyId { get; set; }
    public string ShareableLink { get; set; } = string.Empty;
    public List<string> Tokens { get; set; } = new();
}
