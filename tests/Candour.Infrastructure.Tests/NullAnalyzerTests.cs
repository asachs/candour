namespace Candour.Infrastructure.Tests;

using Candour.Core.ValueObjects;
using Candour.Infrastructure.AI;

public class NullAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsDisabledMessage()
    {
        var analyzer = new NullAnalyzer();
        var data = new AggregateData
        {
            SurveyId = Guid.NewGuid(),
            SurveyTitle = "Test Survey",
            TotalResponses = 42
        };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.Contains("AI analysis is disabled", report.Summary);
        Assert.Contains("appsettings.json", report.Summary);
    }

    [Fact]
    public async Task AnalyzeAsync_SurveyIdMatchesInput()
    {
        var analyzer = new NullAnalyzer();
        var surveyId = Guid.NewGuid();
        var data = new AggregateData { SurveyId = surveyId };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.Equal(surveyId, report.SurveyId);
    }

    [Fact]
    public async Task AnalyzeAsync_ThemesAreEmpty()
    {
        var analyzer = new NullAnalyzer();
        var data = new AggregateData { SurveyId = Guid.NewGuid() };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.Empty(report.Themes);
    }

    [Fact]
    public async Task AnalyzeAsync_KeyInsightsAreEmpty()
    {
        var analyzer = new NullAnalyzer();
        var data = new AggregateData { SurveyId = Guid.NewGuid() };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.Empty(report.KeyInsights);
    }

    [Fact]
    public async Task AnalyzeAsync_SentimentIsNA()
    {
        var analyzer = new NullAnalyzer();
        var data = new AggregateData { SurveyId = Guid.NewGuid() };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.Equal("N/A", report.SentimentOverview);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsNonNullReport()
    {
        var analyzer = new NullAnalyzer();
        var data = new AggregateData { SurveyId = Guid.NewGuid() };

        var report = await analyzer.AnalyzeAsync(data);

        Assert.NotNull(report);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.Themes);
        Assert.NotNull(report.KeyInsights);
        Assert.NotNull(report.SentimentOverview);
    }
}
