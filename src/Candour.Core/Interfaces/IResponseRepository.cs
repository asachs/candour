namespace Candour.Core.Interfaces;

using Candour.Core.Entities;

public interface IResponseRepository : IRepository<SurveyResponse>
{
    Task<int> CountBySurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<List<SurveyResponse>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default);
}
