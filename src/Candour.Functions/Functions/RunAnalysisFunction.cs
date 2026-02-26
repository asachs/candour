namespace Candour.Functions.Functions;

using Candour.Application.Analysis;
using Candour.Shared.Contracts;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class RunAnalysisFunction
{
    private readonly IMediator _mediator;

    public RunAnalysisFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("RunAnalysis")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "surveys/{surveyId}/analyze")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var report = await _mediator.Send(new RunAiAnalysisCommand(id), ct);

        if (report == null)
        {
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            return notFound;
        }

        var dto = new AnalysisReportDto
        {
            SurveyId = report.SurveyId,
            Summary = report.Summary,
            Themes = report.Themes,
            KeyInsights = report.KeyInsights,
            SentimentOverview = report.SentimentOverview,
            GeneratedAt = report.GeneratedAt
        };

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto, ct);
        return response;
    }
}
