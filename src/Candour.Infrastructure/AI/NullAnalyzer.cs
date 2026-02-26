namespace Candour.Infrastructure.AI;

using Candour.Core.Interfaces;
using Candour.Core.ValueObjects;

public class NullAnalyzer : IAiAnalyzer
{
    public Task<AnalysisReport> AnalyzeAsync(AggregateData data, CancellationToken ct = default)
    {
        return Task.FromResult(new AnalysisReport
        {
            SurveyId = data.SurveyId,
            Summary = "AI analysis is disabled. Configure a provider in appsettings.json.",
            Themes = new List<string>(),
            KeyInsights = new List<string>(),
            SentimentOverview = "N/A"
        });
    }
}
