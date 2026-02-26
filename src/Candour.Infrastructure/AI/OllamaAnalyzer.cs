namespace Candour.Infrastructure.AI;

using System.Net.Http.Json;
using System.Text.Json;
using Candour.Core.Interfaces;
using Candour.Core.ValueObjects;

public class OllamaAnalyzer : IAiAnalyzer
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaAnalyzer(HttpClient http, string model = "llama3")
    {
        _http = http;
        _model = model;
    }

    public async Task<AnalysisReport> AnalyzeAsync(AggregateData data, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(data);

        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/generate", request, ct);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var text = result.GetProperty("response").GetString() ?? "";

        return new AnalysisReport
        {
            SurveyId = data.SurveyId,
            Summary = text,
            Themes = new List<string>(),
            KeyInsights = new List<string>(),
            SentimentOverview = "See summary"
        };
    }

    private static string BuildPrompt(AggregateData data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Analyze these anonymous survey results for '{data.SurveyTitle}':");
        sb.AppendLine($"Total responses: {data.TotalResponses}");
        sb.AppendLine();

        foreach (var q in data.Questions)
        {
            sb.AppendLine($"Question: {q.QuestionText} ({q.QuestionType})");
            foreach (var (option, count) in q.OptionCounts)
                sb.AppendLine($"  {option}: {count} ({q.OptionPercentages.GetValueOrDefault(option, 0):P0})");
            if (q.FreeTextAnswers.Count > 0)
            {
                sb.AppendLine("  Free text responses:");
                foreach (var answer in q.FreeTextAnswers)
                    sb.AppendLine($"    - {answer}");
            }
            if (q.AverageRating.HasValue)
                sb.AppendLine($"  Average rating: {q.AverageRating:F1}");
            sb.AppendLine();
        }

        sb.AppendLine("Provide: 1) Summary 2) Key themes 3) Notable insights 4) Overall sentiment");
        return sb.ToString();
    }
}
