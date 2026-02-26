namespace Candour.Application.Responses;

using System.Security.Cryptography;
using System.Text.Json;
using Candour.Core.Interfaces;
using Candour.Core.ValueObjects;
using MediatR;

public record GetAggregateResultsQuery(Guid SurveyId) : IRequest<AggregateResultResponse>;

public record AggregateResultResponse(bool Success, AggregateData? Data = null, string? Error = null);

public class GetAggregateResultsHandler : IRequestHandler<GetAggregateResultsQuery, AggregateResultResponse>
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IResponseRepository _responseRepo;

    public GetAggregateResultsHandler(ISurveyRepository surveyRepo, IResponseRepository responseRepo)
    {
        _surveyRepo = surveyRepo;
        _responseRepo = responseRepo;
    }

    public async Task<AggregateResultResponse> Handle(GetAggregateResultsQuery request, CancellationToken ct)
    {
        var survey = await _surveyRepo.GetWithQuestionsAsync(request.SurveyId, ct);
        if (survey == null)
            return new AggregateResultResponse(false, Error: "Survey not found");

        var responseCount = await _responseRepo.CountBySurveyAsync(request.SurveyId, ct);

        // THRESHOLD GATE: Refuse to return results until anonymity threshold met
        if (responseCount < survey.AnonymityThreshold)
            return new AggregateResultResponse(false,
                Error: $"Insufficient responses. Need {survey.AnonymityThreshold}, have {responseCount}.");

        var responses = await _responseRepo.GetBySurveyAsync(request.SurveyId, ct);

        var questionAggregates = new List<QuestionAggregate>();
        foreach (var question in survey.Questions.OrderBy(q => q.Order))
        {
            var aggregate = new QuestionAggregate
            {
                QuestionText = question.Text,
                QuestionType = question.Type.ToString()
            };

            var questionOptions = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new();

            // Initialize option counts
            foreach (var opt in questionOptions)
            {
                aggregate.OptionCounts[opt] = 0;
                aggregate.OptionPercentages[opt] = 0;
            }

            var ratings = new List<double>();

            foreach (var response in responses)
            {
                var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Answers) ?? new();
                if (!answers.TryGetValue(question.Id.ToString(), out var answer)) continue;

                switch (question.Type)
                {
                    case Core.Enums.QuestionType.MultipleChoice:
                    case Core.Enums.QuestionType.YesNo:
                        if (aggregate.OptionCounts.ContainsKey(answer))
                            aggregate.OptionCounts[answer]++;
                        break;
                    case Core.Enums.QuestionType.FreeText:
                        aggregate.FreeTextAnswers.Add(answer);
                        break;
                    case Core.Enums.QuestionType.Rating:
                        if (double.TryParse(answer, out var rating))
                        {
                            ratings.Add(rating);
                            var key = answer;
                            aggregate.OptionCounts.TryGetValue(key, out var count);
                            aggregate.OptionCounts[key] = count + 1;
                        }
                        break;
                }
            }

            // Calculate percentages
            var total = aggregate.OptionCounts.Values.Sum();
            if (total > 0)
            {
                foreach (var key in aggregate.OptionCounts.Keys)
                    aggregate.OptionPercentages[key] = (double)aggregate.OptionCounts[key] / total * 100;
            }

            if (ratings.Count > 0)
                aggregate.AverageRating = ratings.Average();

            // Shuffle free text answers to prevent ordering correlation (CSPRNG)
            aggregate.FreeTextAnswers = aggregate.FreeTextAnswers
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList();

            questionAggregates.Add(aggregate);
        }

        var data = new AggregateData
        {
            SurveyId = survey.Id,
            SurveyTitle = survey.Title,
            TotalResponses = responseCount,
            Questions = questionAggregates
        };

        return new AggregateResultResponse(true, data);
    }
}
