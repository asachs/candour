namespace Candour.Functions.Functions;

using Candour.Application.Surveys;
using Candour.Shared.Models;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ListSurveysFunction
{
    private readonly IMediator _mediator;

    public ListSurveysFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("ListSurveys")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "surveys")] HttpRequestData req,
        CancellationToken ct)
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

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dtos, ct);
        return response;
    }
}
