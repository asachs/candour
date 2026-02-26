namespace Candour.Api.Endpoints.Survey;

using Candour.Application.Surveys;
using Candour.Shared.Models;
using FastEndpoints;
using MediatR;
using System.Text.Json;

public class GetSurveyEndpoint : EndpointWithoutRequest<SurveyDto>
{
    private readonly IMediator _mediator;

    public GetSurveyEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("/api/surveys/{SurveyId}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get a survey by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var surveyId = Route<Guid>("SurveyId");
        var survey = await _mediator.Send(new GetSurveyQuery(surveyId), ct);

        if (survey == null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var dto = new SurveyDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status.ToString(),
            AnonymityThreshold = survey.AnonymityThreshold,
            TimestampJitterMinutes = survey.TimestampJitterMinutes,
            CreatedAt = survey.CreatedAt,
            Questions = survey.Questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Type = q.Type.ToString(),
                Text = q.Text,
                Options = JsonSerializer.Deserialize<List<string>>(q.Options) ?? new(),
                Required = q.Required,
                Order = q.Order
            }).ToList()
        };

        await HttpContext.Response.SendAsync(dto, cancellation: ct);
    }
}
