namespace Candour.Infrastructure.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ResponseRepository : IResponseRepository
{
    private readonly CandourDbContext _db;

    public ResponseRepository(CandourDbContext db) => _db = db;

    public async Task<SurveyResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Responses.FindAsync(new object[] { id }, ct);

    public async Task<List<SurveyResponse>> ListAsync(CancellationToken ct = default)
        => await _db.Responses.ToListAsync(ct);

    public async Task<SurveyResponse> AddAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        _db.Responses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        _db.Responses.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        _db.Responses.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CountBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => await _db.Responses.CountAsync(r => r.SurveyId == surveyId, ct);

    public async Task<List<SurveyResponse>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => await _db.Responses.Where(r => r.SurveyId == surveyId).ToListAsync(ct);
}
