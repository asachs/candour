namespace Candour.Api.Endpoints.Response;

using Candour.Application.Responses;
using Candour.Shared.Models;
using FastEndpoints;
using MediatR;

public class GetResultsEndpoint : EndpointWithoutRequest<AggregateResultDto>
{
    private readonly IMediator _mediator;

    public GetResultsEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("surveys/{SurveyId}/results");
        Summary(s => s.Summary = "Get aggregate survey results (threshold-gated)");
        Options(x => x.RequireRateLimiting("general"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var surveyId = Route<Guid>("SurveyId");
        var result = await _mediator.Send(new GetAggregateResultsQuery(surveyId), ct);

        if (!result.Success)
        {
            // Return 403 when threshold not met
            await HttpContext.Response.SendAsync(new { error = result.Error }, 403, cancellation: ct);
            return;
        }

        var dto = new AggregateResultDto
        {
            SurveyId = result.Data!.SurveyId,
            SurveyTitle = result.Data.SurveyTitle,
            TotalResponses = result.Data.TotalResponses,
            Questions = result.Data.Questions.Select(q => new QuestionResultDto
            {
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                OptionCounts = q.OptionCounts,
                OptionPercentages = q.OptionPercentages,
                FreeTextAnswers = q.FreeTextAnswers,
                AverageRating = q.AverageRating
            }).ToList()
        };

        await HttpContext.Response.SendAsync(dto, cancellation: ct);
    }
}
