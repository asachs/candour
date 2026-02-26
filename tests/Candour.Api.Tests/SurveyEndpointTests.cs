using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Candour.Shared.Contracts;
using Candour.Shared.Models;

namespace Candour.Api.Tests;

public class SurveyEndpointTests : IClassFixture<CandourApiFactory>, IDisposable
{
    private readonly CandourApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SurveyEndpointTests(CandourApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static CreateSurveyRequest MakeCreateRequest(
        string title = "Test Survey",
        int threshold = 5,
        int jitter = 10)
    {
        return new CreateSurveyRequest
        {
            Title = title,
            Description = "A test survey for integration testing",
            AnonymityThreshold = threshold,
            TimestampJitterMinutes = jitter,
            Questions = new List<CreateQuestionRequest>
            {
                new()
                {
                    Type = "FreeText",
                    Text = "What do you think?",
                    Options = new List<string>(),
                    Required = true,
                    Order = 0
                }
            }
        };
    }

    [Fact]
    public async Task CreateSurvey_Returns201_WithSurveyDto()
    {
        var request = MakeCreateRequest("Create Test");

        var response = await _client.PostAsJsonAsync("/api/surveys", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("Create Test", dto.Title);
        Assert.Equal("A test survey for integration testing", dto.Description);
        Assert.Equal("Draft", dto.Status);
        Assert.Equal(5, dto.AnonymityThreshold);
        Assert.Equal(10, dto.TimestampJitterMinutes);
        Assert.NotEmpty(dto.Questions);
        Assert.Equal("FreeText", dto.Questions[0].Type);
        Assert.Equal("What do you think?", dto.Questions[0].Text);
    }

    [Fact]
    public async Task ListSurveys_ReturnsListIncludingCreatedSurvey()
    {
        // Create a survey first
        var request = MakeCreateRequest("List Test");
        var createResponse = await _client.PostAsJsonAsync("/api/surveys", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(created);

        // List surveys
        var listResponse = await _client.GetAsync("/api/surveys");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var surveys = await listResponse.Content.ReadFromJsonAsync<List<SurveyDto>>(JsonOptions);
        Assert.NotNull(surveys);
        Assert.Contains(surveys, s => s.Id == created.Id && s.Title == "List Test");
    }

    [Fact]
    public async Task GetSurveyById_ReturnsSurvey_WhenExists()
    {
        // Create a survey
        var request = MakeCreateRequest("Get By Id Test");
        var createResponse = await _client.PostAsJsonAsync("/api/surveys", request);
        var created = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(created);

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/surveys/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var dto = await getResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto.Id);
        Assert.Equal("Get By Id Test", dto.Title);
        Assert.NotEmpty(dto.Questions);
    }

    [Fact]
    public async Task GetSurveyById_Returns404_WhenNotFound()
    {
        var randomId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/surveys/{randomId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishSurvey_ReturnsTokens()
    {
        // Create a survey
        var createResponse = await _client.PostAsJsonAsync("/api/surveys", MakeCreateRequest("Publish Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(created);

        // Publish with 10 tokens
        var publishResponse = await _client.PostAsJsonAsync(
            $"/api/surveys/{created.Id}/publish",
            new { tokenCount = 10 });

        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var link = await publishResponse.Content.ReadFromJsonAsync<SurveyLinkResponse>(JsonOptions);
        Assert.NotNull(link);
        Assert.Equal(created.Id, link.SurveyId);
        Assert.NotEmpty(link.ShareableLink);
        Assert.Equal(10, link.Tokens.Count);
        Assert.All(link.Tokens, t => Assert.False(string.IsNullOrWhiteSpace(t)));
    }

    [Fact]
    public async Task FullLifecycle_Create_Publish_SubmitResponses_GetResults()
    {
        // Step 1: Create survey with low threshold for testing
        var createRequest = MakeCreateRequest("Lifecycle Test", threshold: 2, jitter: 0);
        var createResponse = await _client.PostAsJsonAsync("/api/surveys", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var survey = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(survey);
        var questionId = survey.Questions[0].Id.ToString();

        // Step 2: Publish to get tokens
        var publishResponse = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/publish",
            new { tokenCount = 5 });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var published = await publishResponse.Content.ReadFromJsonAsync<SurveyLinkResponse>(JsonOptions);
        Assert.NotNull(published);
        Assert.Equal(5, published.Tokens.Count);

        // Step 3: Results should be gated before threshold is met
        var earlyResultsResponse = await _client.GetAsync($"/api/surveys/{survey.Id}/results");
        Assert.Equal(HttpStatusCode.Forbidden, earlyResultsResponse.StatusCode);

        // Step 4: Submit responses to meet threshold (threshold = 2)
        for (int i = 0; i < 2; i++)
        {
            var submitRequest = new SubmitResponseRequest
            {
                Token = published.Tokens[i],
                Answers = new Dictionary<string, string>
                {
                    { questionId, $"Answer from respondent {i}" }
                }
            };

            var submitResponse = await _client.PostAsJsonAsync(
                $"/api/surveys/{survey.Id}/responses",
                submitRequest);
            Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        }

        // Step 5: Results should now be available (threshold met)
        var resultsResponse = await _client.GetAsync($"/api/surveys/{survey.Id}/results");
        Assert.Equal(HttpStatusCode.OK, resultsResponse.StatusCode);

        var results = await resultsResponse.Content.ReadFromJsonAsync<AggregateResultDto>(JsonOptions);
        Assert.NotNull(results);
        Assert.Equal(survey.Id, results.SurveyId);
        Assert.Equal(2, results.TotalResponses);
        Assert.NotEmpty(results.Questions);
        Assert.Equal("What do you think?", results.Questions[0].QuestionText);
        Assert.Equal(2, results.Questions[0].FreeTextAnswers.Count);
    }

    [Fact]
    public async Task SubmitResponse_Returns400_WithInvalidToken()
    {
        // Create and publish a survey
        var createResponse = await _client.PostAsJsonAsync(
            "/api/surveys", MakeCreateRequest("Bad Token Test"));
        var survey = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(survey);

        await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/publish",
            new { tokenCount = 1 });

        // Submit with an invalid token (not valid base64 of 32 bytes)
        var submitRequest = new SubmitResponseRequest
        {
            Token = "not-a-valid-token",
            Answers = new Dictionary<string, string>
            {
                { survey.Questions[0].Id.ToString(), "My answer" }
            }
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/responses",
            submitRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitResponse_Returns400_WhenTokenReused()
    {
        // Create and publish
        var createResponse = await _client.PostAsJsonAsync(
            "/api/surveys", MakeCreateRequest("Reuse Token Test"));
        var survey = await createResponse.Content.ReadFromJsonAsync<SurveyDto>(JsonOptions);
        Assert.NotNull(survey);

        var publishResponse = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/publish",
            new { tokenCount = 3 });
        var published = await publishResponse.Content.ReadFromJsonAsync<SurveyLinkResponse>(JsonOptions);
        Assert.NotNull(published);

        var questionId = survey.Questions[0].Id.ToString();
        var token = published.Tokens[0];

        // First submission should succeed
        var firstSubmit = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/responses",
            new SubmitResponseRequest
            {
                Token = token,
                Answers = new Dictionary<string, string> { { questionId, "First" } }
            });
        Assert.Equal(HttpStatusCode.OK, firstSubmit.StatusCode);

        // Second submission with same token should fail
        var secondSubmit = await _client.PostAsJsonAsync(
            $"/api/surveys/{survey.Id}/responses",
            new SubmitResponseRequest
            {
                Token = token,
                Answers = new Dictionary<string, string> { { questionId, "Second" } }
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondSubmit.StatusCode);
    }
}
