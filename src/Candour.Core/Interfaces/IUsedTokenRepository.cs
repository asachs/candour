namespace Candour.Core.Interfaces;

public interface IUsedTokenRepository
{
    Task<bool> ExistsAsync(string tokenHash, Guid surveyId, CancellationToken ct = default);
    Task AddAsync(string tokenHash, Guid surveyId, CancellationToken ct = default);
}
