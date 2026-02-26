namespace Candour.Infrastructure.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

public class UsedTokenRepository : IUsedTokenRepository
{
    private readonly CandourDbContext _db;

    public UsedTokenRepository(CandourDbContext db) => _db = db;

    public async Task<bool> ExistsAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
        => await _db.UsedTokens.AnyAsync(t => t.TokenHash == tokenHash && t.SurveyId == surveyId, ct);

    public async Task AddAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
    {
        _db.UsedTokens.Add(new UsedToken
        {
            TokenHash = tokenHash,
            SurveyId = surveyId
        });
        await _db.SaveChangesAsync(ct);
    }
}
