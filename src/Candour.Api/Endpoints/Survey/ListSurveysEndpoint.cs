namespace Candour.Api.Endpoints.Survey;

using Candour.Application.Surveys;
using Candour.Shared.Models;
using FastEndpoints;
using MediatR;

public class ListSurveysEndpoint : EndpointWithoutRequest<List<SurveyDto>>
{
    private readonly IMediator _mediator;

    public ListSurveysEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("/api/surveys");
        AllowAnonymous();
        Summary(s => s.Summary = "List all surveys");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var surveys = await _mediator.Send(new ListSurveysQuery(), ct);

        var dtos = surveys.Select(s => new SurveyDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Status = s.Status.ToString(),
            AnonymityThreshold = s.AnonymityThreshold,
            TimestampJitterMinutes = s.TimestampJitterMinutes,
            CreatedAt = s.CreatedAt
        }).ToList();

        await HttpContext.Response.SendAsync(dtos, cancellation: ct);
    }
}
