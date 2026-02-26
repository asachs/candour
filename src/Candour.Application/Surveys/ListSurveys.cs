namespace Candour.Application.Surveys;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using MediatR;

public record ListSurveysQuery(string? CreatorId = null) : IRequest<List<Survey>>;

public class ListSurveysHandler : IRequestHandler<ListSurveysQuery, List<Survey>>
{
    private readonly ISurveyRepository _repo;

    public ListSurveysHandler(ISurveyRepository repo) => _repo = repo;

    public async Task<List<Survey>> Handle(ListSurveysQuery request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.CreatorId))
            return await _repo.GetByCreatorAsync(request.CreatorId, ct);
        return await _repo.ListAsync(ct);
    }
}
