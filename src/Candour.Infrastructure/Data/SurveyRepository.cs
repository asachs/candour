namespace Candour.Infrastructure.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SurveyRepository : ISurveyRepository
{
    private readonly CandourDbContext _db;

    public SurveyRepository(CandourDbContext db) => _db = db;

    public async Task<Survey?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Surveys.FindAsync(new object[] { id }, ct);

    public async Task<List<Survey>> ListAsync(CancellationToken ct = default)
        => await _db.Surveys.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task<Survey> AddAsync(Survey entity, CancellationToken ct = default)
    {
        _db.Surveys.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Survey entity, CancellationToken ct = default)
    {
        _db.Surveys.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Survey entity, CancellationToken ct = default)
    {
        _db.Surveys.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Survey?> GetWithQuestionsAsync(Guid id, CancellationToken ct = default)
        => await _db.Surveys.Include(s => s.Questions.OrderBy(q => q.Order))
                            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<Survey>> GetByCreatorAsync(string creatorId, CancellationToken ct = default)
        => await _db.Surveys.Where(s => s.CreatorId == creatorId)
                            .OrderByDescending(s => s.CreatedAt)
                            .ToListAsync(ct);
}
