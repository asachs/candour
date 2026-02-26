namespace Candour.Functions.Functions;

using System.Text.Json;
using Candour.Application.Responses;
using Candour.Shared.Contracts;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class SubmitResponseFunction
{
    private readonly IMediator _mediator;

    public SubmitResponseFunction(IMediator mediator) => _mediator = mediator;

    [Function("SubmitResponse")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "surveys/{surveyId}/responses")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var body = await JsonSerializer.DeserializeAsync<SubmitResponseRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        if (body == null)
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var result = await _mediator.Send(new SubmitResponseCommand(id, body.Token, body.Answers), ct);

        if (result.Success)
        {
            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            return ok;
        }

        var error = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        await error.WriteAsJsonAsync(new { error = result.Error }, ct);
        return error;
    }
}
