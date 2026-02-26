using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Candour.Shared.Contracts;
using Candour.Shared.Models;

namespace Candour.Api.Tests;

public class AnonymityMiddlewareIntegrationTests : IClassFixture<CandourApiFactory>, IDisposable
{
    private readonly CandourApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnonymityMiddlewareIntegrationTests(CandourApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key-for-integration-tests");
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    /// <summary>
    /// Creates a survey, publishes it, and returns the survey DTO and tokens.
    /// Helper for tests that need a published survey to submit responses against.
    /// </summary>
    private async Task<(SurveyDto Survey, List<string> Tokens)> CreateAndPublishSurvey()
    {
        var createRequest = new CreateSurveyRequest
        {
            Title = "Anonymity Test Survey",
            Description = "Testing middleware anonymity",
            AnonymityThreshold = 1,
            TimestampJitterMinutes = 0,
            Questions = new List<CreateQuestionRequest>
            {
                new()
                {
                    Type = "FreeText",
                    Text = "Anonymous question?",
                    Options = new List<string>(),
                    Required = true,
                    Order = 0
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/surveys", createRequest);
        var survey = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);

        var publishResponse = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey!.Id}/publish",
            new { tokenCount = 5 });
        var published = await publishResponse.Content.ReadFromJsonAsync<SurveyLinkResponse>(JsonOptions);

        return (survey, published!.Tokens);
    }

    [Fact]
    public async Task ResponseEndpoint_StripsForwardedHeaders_StillProcessesRequest()
    {
        // Arrange: create a published survey with tokens
        var (survey, tokens) = await CreateAndPublishSurvey();

        var submitRequest = new SubmitResponseRequest
        {
            Token = tokens[0],
            Answers = new Dictionary<string, string>
            {
                { survey.Questions[0].Id.ToString(), "My anonymous answer" }
            }
        };

        // Act: send response with tracking headers that should be stripped
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/surveys/{survey.Id}/responses")
        {
            Content = JsonContent.Create(submitRequest)
        };
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "192.168.1.100");
        request.Headers.TryAddWithoutValidation("X-Real-IP", "10.0.0.1");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "evil-tracker.com");
        request.Headers.TryAddWithoutValidation("X-Client-IP", "172.16.0.1");
        request.Headers.TryAddWithoutValidation("CF-Connecting-IP", "203.0.113.50");

        var response = await _client.SendAsync(request);

        // Assert: request still processes successfully (headers stripped, not rejected)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify Set-Cookie is not present in response
        Assert.False(
            response.Headers.Contains("Set-Cookie"),
            "Response endpoint must not return Set-Cookie headers");
    }

    [Fact]
    public async Task ResponseEndpoint_DoesNotReturnSetCookie()
    {
        var (survey, tokens) = await CreateAndPublishSurvey();

        var submitRequest = new SubmitResponseRequest
        {
            Token = tokens[1],
            Answers = new Dictionary<string, string>
            {
                { survey.Questions[0].Id.ToString(), "Cookie-free answer" }
            }
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/responses",
            submitRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The middleware should strip Set-Cookie from response
        Assert.False(
            response.Headers.Contains("Set-Cookie"),
            "Set-Cookie header must be stripped on response endpoints");
    }

    [Fact]
    public async Task NonSensitiveEndpoint_PreservesNormalBehavior()
    {
        // Non-response endpoints (e.g., /api/surveys) should work normally
        // The middleware only activates for paths containing /api/responses or /survey/
        var createRequest = new CreateSurveyRequest
        {
            Title = "Normal Endpoint Test",
            Description = "Testing non-sensitive path",
            AnonymityThreshold = 5,
            TimestampJitterMinutes = 10,
            Questions = new List<CreateQuestionRequest>
            {
                new()
                {
                    Type = "FreeText",
                    Text = "Normal question",
                    Options = new List<string>(),
                    Required = true,
                    Order = 0
                }
            }
        };

        // Add headers that would be stripped on sensitive routes
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/surveys")
        {
            Content = JsonContent.Create(createRequest)
        };
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "10.0.0.1");

        var response = await _client.SendAsync(request);

        // Non-sensitive endpoint should function normally
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Normal Endpoint Test", dto.Title);
    }

    [Fact]
    public async Task ListSurveysEndpoint_NotAffectedByMiddleware()
    {
        // GET /api/surveys should not trigger the anonymity middleware
        // (path does not contain /api/responses or /survey/)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/surveys");
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "192.168.1.1");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
