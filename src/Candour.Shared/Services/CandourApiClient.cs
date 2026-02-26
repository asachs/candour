namespace Candour.Shared.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Candour.Shared.Contracts;
using Candour.Shared.Models;

public class CandourApiClient : ICandourApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CandourApiClient(HttpClient http) => _http = http;

    public async Task<SurveyDto?> GetSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/surveys/{surveyId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions, ct);
    }

    public async Task<List<SurveyDto>> ListSurveysAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/surveys", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SurveyDto>>(JsonOptions, ct) ?? new();
    }

    public async Task<SurveyDto> CreateSurveyAsync(CreateSurveyRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/surveys", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize survey response");
    }

    public async Task<SurveyLinkResponse> PublishSurveyAsync(Guid surveyId, int tokenCount = 100, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/surveys/{surveyId}/publish", new { tokenCount }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SurveyLinkResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize publish response");
    }

    public async Task<SubmitResult> SubmitResponseAsync(Guid surveyId, SubmitResponseRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/surveys/{surveyId}/responses", request, ct);
        if (response.IsSuccessStatusCode)
            return new SubmitResult(true);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, ct);
        return new SubmitResult(false, error?.Error ?? "Unknown error");
    }

    public async Task<AggregateResultDto?> GetResultsAsync(Guid surveyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/surveys/{surveyId}/results", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AggregateResultDto>(JsonOptions, ct);
    }

    public async Task<string?> GetResultsErrorAsync(Guid surveyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/surveys/{surveyId}/results", ct);
        if (response.IsSuccessStatusCode) return null;
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, ct);
        return error?.Error;
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(Guid surveyId, string token, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/surveys/{surveyId}/validate-token", new { token }, ct);
        var result = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>(JsonOptions, ct);
        return result ?? new ValidateTokenResponse(false, "Failed to validate token");
    }

    public async Task<AnalysisReportDto?> RunAnalysisAsync(Guid surveyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/surveys/{surveyId}/analyze", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AnalysisReportDto>(JsonOptions, ct);
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
}
