namespace Candour.Infrastructure.Tests;

using System.Net;
using System.Text.Json;
using Candour.Core.ValueObjects;
using Candour.Infrastructure.AI;

public class OllamaAnalyzerTests
{
    private static AggregateData MakeTestData()
    {
        return new AggregateData
        {
            SurveyId = Guid.NewGuid(),
            SurveyTitle = "Test Survey",
            TotalResponses = 10,
            Questions = new List<QuestionAggregate>
            {
                new()
                {
                    QuestionText = "How satisfied?",
                    QuestionType = "Rating",
                    OptionCounts = new Dictionary<string, int> { { "5", 6 }, { "4", 4 } },
                    OptionPercentages = new Dictionary<string, double> { { "5", 60 }, { "4", 40 } },
                    FreeTextAnswers = new List<string>(),
                    AverageRating = 4.6
                },
                new()
                {
                    QuestionText = "Any comments?",
                    QuestionType = "FreeText",
                    OptionCounts = new Dictionary<string, int>(),
                    OptionPercentages = new Dictionary<string, double>(),
                    FreeTextAnswers = new List<string> { "Great service", "Needs improvement" },
                    AverageRating = null
                }
            }
        };
    }

    private static HttpClient CreateMockHttpClient(string responseText)
    {
        var json = JsonSerializer.Serialize(new { response = responseText });
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        return client;
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsReportWithSummary()
    {
        var data = MakeTestData();
        var client = CreateMockHttpClient("Overall positive sentiment");
        var analyzer = new OllamaAnalyzer(client, "llama3");

        var report = await analyzer.AnalyzeAsync(data);

        Assert.NotNull(report);
        Assert.Equal(data.SurveyId, report.SurveyId);
        Assert.Equal("Overall positive sentiment", report.Summary);
        Assert.Equal("See summary", report.SentimentOverview);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmptyThemesAndInsights()
    {
        var data = MakeTestData();
        var client = CreateMockHttpClient("Some analysis");
        var analyzer = new OllamaAnalyzer(client);

        var report = await analyzer.AnalyzeAsync(data);

        Assert.NotNull(report.Themes);
        Assert.Empty(report.Themes);
        Assert.NotNull(report.KeyInsights);
        Assert.Empty(report.KeyInsights);
    }

    [Fact]
    public async Task AnalyzeAsync_PostsToCorrectEndpoint()
    {
        var data = MakeTestData();
        var handler = new MockHttpHandler(JsonSerializer.Serialize(new { response = "test" }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var analyzer = new OllamaAnalyzer(client, "test-model");

        await analyzer.AnalyzeAsync(data);

        Assert.Equal("/api/generate", handler.LastRequestUri?.AbsolutePath);
        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.Equal("test-model", body.GetProperty("model").GetString());
        Assert.False(body.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task AnalyzeAsync_BuildsPromptWithQuestionData()
    {
        var data = MakeTestData();
        var handler = new MockHttpHandler(JsonSerializer.Serialize(new { response = "analysis" }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var analyzer = new OllamaAnalyzer(client);

        await analyzer.AnalyzeAsync(data);

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        var prompt = body.GetProperty("prompt").GetString()!;
        Assert.Contains("Test Survey", prompt);
        Assert.Contains("How satisfied?", prompt);
        Assert.Contains("Any comments?", prompt);
        Assert.Contains("Great service", prompt);
        Assert.Contains("Average rating: 4.6", prompt);
        Assert.Contains("Total responses: 10", prompt);
    }

    [Fact]
    public async Task AnalyzeAsync_DefaultModelIsLlama3()
    {
        var handler = new MockHttpHandler(JsonSerializer.Serialize(new { response = "test" }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var analyzer = new OllamaAnalyzer(client);

        await analyzer.AnalyzeAsync(MakeTestData());

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.Equal("llama3", body.GetProperty("model").GetString());
    }

    private class MockHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        public MockHttpHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
