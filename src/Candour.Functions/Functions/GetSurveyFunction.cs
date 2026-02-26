namespace Candour.Functions.Functions;

using System.Text.Json;
using Candour.Application.Surveys;
using Candour.Shared.Models;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class GetSurveyFunction
{
    private readonly IMediator _mediator;

    public GetSurveyFunction(IMediator mediator) => _mediator = mediator;

    [Function("GetSurvey")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "surveys/{surveyId}")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var survey = await _mediator.Send(new GetSurveyQuery(id), ct);

        if (survey == null)
        {
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            return notFound;
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

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto, ct);
        return response;
    }
}
