namespace Candour.Functions.Functions;

using Candour.Application.Responses;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ExportCsvFunction
{
    private readonly IMediator _mediator;

    public ExportCsvFunction(IMediator mediator) => _mediator = mediator;

    [Function("ExportCsv")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "surveys/{surveyId}/export")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var result = await _mediator.Send(new ExportCsvQuery(id), ct);

        if (!result.Success)
        {
            var forbidden = req.CreateResponse(System.Net.HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = result.Error }, ct);
            return forbidden;
        }

        var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
        ok.Headers.Add("Content-Type", "text/csv; charset=utf-8");
        ok.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
        ok.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
        await ok.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(result.CsvContent!), ct);
        return ok;
    }
}
