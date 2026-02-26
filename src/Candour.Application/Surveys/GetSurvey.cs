namespace Candour.Application.Surveys;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using MediatR;

public record GetSurveyQuery(Guid SurveyId) : IRequest<Survey?>;

public class GetSurveyHandler : IRequestHandler<GetSurveyQuery, Survey?>
{
    private readonly ISurveyRepository _repo;

    public GetSurveyHandler(ISurveyRepository repo) => _repo = repo;

    public async Task<Survey?> Handle(GetSurveyQuery request, CancellationToken ct)
        => await _repo.GetWithQuestionsAsync(request.SurveyId, ct);
}
