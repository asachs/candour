namespace Candour.Application.Responses;

using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using MediatR;

public record SubmitResponseCommand(
    Guid SurveyId,
    string Token,
    Dictionary<string, string> Answers
) : IRequest<SubmitResponseResult>;

public record SubmitResponseResult(bool Success, string? Error = null);

public class SubmitResponseHandler : IRequestHandler<SubmitResponseCommand, SubmitResponseResult>
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IResponseRepository _responseRepo;
    private readonly ITokenService _tokenService;
    private readonly ITimestampJitterService _jitterService;

    public SubmitResponseHandler(
        ISurveyRepository surveyRepo,
        IResponseRepository responseRepo,
        ITokenService tokenService,
        ITimestampJitterService jitterService)
    {
        _surveyRepo = surveyRepo;
        _responseRepo = responseRepo;
        _tokenService = tokenService;
        _jitterService = jitterService;
    }

    public async Task<SubmitResponseResult> Handle(SubmitResponseCommand request, CancellationToken ct)
    {
        var survey = await _surveyRepo.GetByIdAsync(request.SurveyId, ct);
        if (survey == null)
            return new SubmitResponseResult(false, "Survey not found");

        if (survey.Status != SurveyStatus.Active)
            return new SubmitResponseResult(false, "Survey is not active");

        // Validate and check token
        if (!_tokenService.ValidateToken(request.Token, survey.BatchSecret))
            return new SubmitResponseResult(false, "Invalid token");

        var tokenHash = _tokenService.HashToken(request.Token);
        if (await _tokenService.IsTokenUsedAsync(tokenHash, request.SurveyId, ct))
            return new SubmitResponseResult(false, "Token already used");

        // Mark token as used (UsedTokens table -- SEPARATE from Responses)
        await _tokenService.MarkTokenUsedAsync(tokenHash, request.SurveyId, ct);

        // Store response with jittered timestamp (Responses table -- NO link to UsedTokens)
        var response = new SurveyResponse
        {
            SurveyId = request.SurveyId,
            Answers = System.Text.Json.JsonSerializer.Serialize(request.Answers),
            SubmittedAt = _jitterService.ApplyJitter(DateTime.UtcNow, survey.TimestampJitterMinutes)
        };

        await _responseRepo.AddAsync(response, ct);
        return new SubmitResponseResult(true);
    }
}
