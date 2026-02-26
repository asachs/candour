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
    private readonly IBatchSecretProtector _protector;

    public PublishSurveyHandler(ISurveyRepository repo, ITokenService tokenService, IBatchSecretProtector protector)
    {
        _repo = repo;
        _tokenService = tokenService;
        _protector = protector;
    }

    public async Task<PublishSurveyResult> Handle(PublishSurveyCommand request, CancellationToken ct)
    {
        var survey = await _repo.GetByIdAsync(request.SurveyId, ct)
            ?? throw new InvalidOperationException("Survey not found");

        if (request.TokenCount > 10_000)
            throw new InvalidOperationException("Token count cannot exceed 10,000");

        survey.Status = SurveyStatus.Active;
        await _repo.UpdateAsync(survey, ct);

        var secret = _protector.Unprotect(survey.BatchSecret);
        var tokens = Enumerable.Range(0, request.TokenCount)
            .Select(_ => _tokenService.GenerateToken(secret))
            .ToList();

        return new PublishSurveyResult(survey.Id, tokens);
    }
}
