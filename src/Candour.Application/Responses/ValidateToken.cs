namespace Candour.Application.Responses;

using Candour.Core.Enums;
using Candour.Core.Interfaces;
using MediatR;

public record ValidateTokenQuery(Guid SurveyId, string Token) : IRequest<ValidateTokenResult>;

public record ValidateTokenResult(bool Valid, string? Error = null);

public class ValidateTokenHandler : IRequestHandler<ValidateTokenQuery, ValidateTokenResult>
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly ITokenService _tokenService;
    private readonly IBatchSecretProtector _protector;

    public ValidateTokenHandler(
        ISurveyRepository surveyRepo,
        ITokenService tokenService,
        IBatchSecretProtector protector)
    {
        _surveyRepo = surveyRepo;
        _tokenService = tokenService;
        _protector = protector;
    }

    public async Task<ValidateTokenResult> Handle(ValidateTokenQuery request, CancellationToken ct)
    {
        var survey = await _surveyRepo.GetByIdAsync(request.SurveyId, ct);
        if (survey == null)
            return new ValidateTokenResult(false, "Survey not found");

        if (survey.Status != SurveyStatus.Active)
            return new ValidateTokenResult(false, "Survey is not active");

        var secret = _protector.Unprotect(survey.BatchSecret);
        if (!_tokenService.ValidateToken(request.Token, secret))
            return new ValidateTokenResult(false, "Invalid token");

        var tokenHash = _tokenService.HashToken(request.Token);
        if (await _tokenService.IsTokenUsedAsync(tokenHash, request.SurveyId, ct))
            return new ValidateTokenResult(false, "Token already used");

        return new ValidateTokenResult(true);
    }
}
