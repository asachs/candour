namespace Candour.Core.Interfaces;

using Candour.Core.Entities;

public interface ISurveyRepository : IRepository<Survey>
{
    Task<Survey?> GetWithQuestionsAsync(Guid id, CancellationToken ct = default);
    Task<List<Survey>> GetByCreatorAsync(string creatorId, CancellationToken ct = default);
}
