namespace Candour.Application.Surveys;

using Candour.Core.Enums;
using Candour.Core.Interfaces;
using MediatR;

public record CloseSurveyCommand(Guid SurveyId) : IRequest<bool>;

public class CloseSurveyHandler : IRequestHandler<CloseSurveyCommand, bool>
{
    private readonly ISurveyRepository _repo;

    public CloseSurveyHandler(ISurveyRepository repo) => _repo = repo;

    public async Task<bool> Handle(CloseSurveyCommand request, CancellationToken ct)
    {
        var survey = await _repo.GetByIdAsync(request.SurveyId, ct);
        if (survey == null) return false;

        survey.Status = SurveyStatus.Closed;
        await _repo.UpdateAsync(survey, ct);
        return true;
    }
}
