namespace Candour.Shared.Services;

using Candour.Shared.Contracts;
using Candour.Shared.Models;

public interface ICandourApiClient
{
    Task<SurveyDto?> GetSurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<List<SurveyDto>> ListSurveysAsync(CancellationToken ct = default);
    Task<SurveyDto> CreateSurveyAsync(CreateSurveyRequest request, CancellationToken ct = default);
    Task<SurveyLinkResponse> PublishSurveyAsync(Guid surveyId, int tokenCount = 100, CancellationToken ct = default);
    Task<SubmitResult> SubmitResponseAsync(Guid surveyId, SubmitResponseRequest request, CancellationToken ct = default);
    Task<AggregateResultDto?> GetResultsAsync(Guid surveyId, CancellationToken ct = default);
    Task<string?> GetResultsErrorAsync(Guid surveyId, CancellationToken ct = default);
    Task<ValidateTokenResponse> ValidateTokenAsync(Guid surveyId, string token, CancellationToken ct = default);
    Task<AnalysisReportDto?> RunAnalysisAsync(Guid surveyId, CancellationToken ct = default);
}

public record SubmitResult(bool Success, string? Error = null);
public record ValidateTokenResponse(bool Valid, string? Error = null);
