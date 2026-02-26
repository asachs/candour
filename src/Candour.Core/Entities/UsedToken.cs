namespace Candour.Core.Entities;

/// <summary>
/// Tracks used tokens via their SHA256 hash for duplicate prevention.
/// The original token is NEVER stored — only its one-way hash.
/// This table is NEVER joined to SurveyResponse.
/// </summary>
public class UsedToken
{
    public string TokenHash { get; set; } = string.Empty; // SHA256(token), primary key
    public Guid SurveyId { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
