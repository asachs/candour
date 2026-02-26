namespace Candour.Application.Surveys;

using Candour.Core.Enums;
using Candour.Core.Interfaces;
using MediatR;

public record PublishSurveyCommand(Guid SurveyId, int TokenCount = 100) : IRequest<PublishSurveyResult>;

public record PublishSurveyResult(Guid SurveyId, List<string> Tokens);

public class PublishSurveyHandler : IRequestHandler<PublishSurveyCommand, PublishSurveyResult>
{
    private readonly ISurveyRepository _repo;
    private readonly ITokenService _tokenService;

    public PublishSurveyHandler(ISurveyRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    public async Task<PublishSurveyResult> Handle(PublishSurveyCommand request, CancellationToken ct)
    {
        var survey = await _repo.GetByIdAsync(request.SurveyId, ct)
            ?? throw new InvalidOperationException("Survey not found");

        survey.Status = SurveyStatus.Active;
        await _repo.UpdateAsync(survey, ct);

        var tokens = Enumerable.Range(0, request.TokenCount)
            .Select(_ => _tokenService.GenerateToken(survey.BatchSecret))
            .ToList();

        return new PublishSurveyResult(survey.Id, tokens);
    }
}
