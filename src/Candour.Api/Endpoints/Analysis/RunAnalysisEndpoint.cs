namespace Candour.Api.Endpoints.Analysis;

using Candour.Application.Analysis;
using Candour.Shared.Contracts;
using FastEndpoints;
using MediatR;

public class RunAnalysisEndpoint : EndpointWithoutRequest<AnalysisReportDto>
{
    private readonly IMediator _mediator;

    public RunAnalysisEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("surveys/{SurveyId}/analyze");
        AllowAnonymous();
        Summary(s => s.Summary = "Run AI analysis on survey results");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var surveyId = Route<Guid>("SurveyId");
        var report = await _mediator.Send(new RunAiAnalysisCommand(surveyId), ct);

        if (report == null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(new AnalysisReportDto
        {
            SurveyId = report.SurveyId,
            Summary = report.Summary,
            Themes = report.Themes,
            KeyInsights = report.KeyInsights,
            SentimentOverview = report.SentimentOverview,
            GeneratedAt = report.GeneratedAt
        }, cancellation: ct);
    }
}
