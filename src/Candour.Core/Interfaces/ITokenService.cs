namespace Candour.Core.Interfaces;

public interface ITokenService
{
    string GenerateBatchSecret();
    string GenerateToken(string batchSecret);
    string HashToken(string token);
    bool ValidateToken(string token, string batchSecret);
    Task<bool> IsTokenUsedAsync(string tokenHash, Guid surveyId, CancellationToken ct = default);
    Task MarkTokenUsedAsync(string tokenHash, Guid surveyId, CancellationToken ct = default);
}
