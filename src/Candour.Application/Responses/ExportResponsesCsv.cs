namespace Candour.Application.Responses;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Candour.Core.Interfaces;
using MediatR;

public record ExportCsvQuery(Guid SurveyId) : IRequest<ExportCsvResult>;

public record ExportCsvResult(bool Success, string? CsvContent = null, string? FileName = null, string? Error = null);

public class ExportCsvHandler : IRequestHandler<ExportCsvQuery, ExportCsvResult>
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IResponseRepository _responseRepo;

    public ExportCsvHandler(ISurveyRepository surveyRepo, IResponseRepository responseRepo)
    {
        _surveyRepo = surveyRepo;
        _responseRepo = responseRepo;
    }

    public async Task<ExportCsvResult> Handle(ExportCsvQuery request, CancellationToken ct)
    {
        var survey = await _surveyRepo.GetWithQuestionsAsync(request.SurveyId, ct);
        if (survey == null)
            return new ExportCsvResult(false, Error: "Survey not found");

        var responseCount = await _responseRepo.CountBySurveyAsync(request.SurveyId, ct);

        // THRESHOLD GATE: Refuse to export until anonymity threshold met
        if (responseCount < survey.AnonymityThreshold)
            return new ExportCsvResult(false,
                Error: $"Insufficient responses. Need {survey.AnonymityThreshold}, have {responseCount}.");

        var responses = await _responseRepo.GetBySurveyAsync(request.SurveyId, ct);

        // Shuffle responses using CSPRNG to prevent ordering correlation
        var shuffled = responses
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .ToList();

        var questions = survey.Questions.OrderBy(q => q.Order).ToList();

        var sb = new StringBuilder();

        // Header row: question texts + "Submitted At"
        for (var i = 0; i < questions.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(EscapeCsvField(questions[i].Text));
        }
        sb.Append(",Submitted At");
        sb.AppendLine();

        // Data rows
        foreach (var response in shuffled)
        {
            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Answers) ?? new();

            for (var i = 0; i < questions.Count; i++)
            {
                if (i > 0) sb.Append(',');
                answers.TryGetValue(questions[i].Id.ToString(), out var answer);
                sb.Append(EscapeCsvField(answer ?? ""));
            }

            sb.Append(',');
            sb.Append(EscapeCsvField(response.SubmittedAt.ToString("O")));
            sb.AppendLine();
        }

        var fileName = SanitizeFileName(survey.Title) + "-responses.csv";

        return new ExportCsvResult(true, sb.ToString(), fileName);
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private static string SanitizeFileName(string title)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(title.Length);
        foreach (var c in title)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }
        return sb.ToString();
    }
}
