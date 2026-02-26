namespace Candour.Functions.Functions;

using Candour.Application.Responses;
using Candour.Shared.Models;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class GetResultsFunction
{
    private readonly IMediator _mediator;

    public GetResultsFunction(IMediator mediator) => _mediator = mediator;

    [Function("GetResults")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "surveys/{surveyId}/results")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var result = await _mediator.Send(new GetAggregateResultsQuery(id), ct);

        if (!result.Success)
        {
            var forbidden = req.CreateResponse(System.Net.HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = result.Error }, ct);
            return forbidden;
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

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto, ct);
        return response;
    }
}
