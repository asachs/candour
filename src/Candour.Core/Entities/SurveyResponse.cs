namespace Candour.Core.Entities;

/// <summary>
/// Survey response entity. By design, this entity contains ZERO identity fields.
/// No RespondentId, IpAddress, UserAgent, or TokenReference — anonymity is architectural.
/// </summary>
public class SurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Random UUID, not auto-increment
    public Guid SurveyId { get; set; }
    public string Answers { get; set; } = "{}"; // JSON
    public DateTime SubmittedAt { get; set; } // Jittered before storage

    // DELIBERATELY NO: RespondentId, IpAddress, UserAgent, TokenReference
    // Anonymity is enforced by the absence of these fields.
}
