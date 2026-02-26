namespace Candour.Functions.Functions;

using System.Text.Json;
using Candour.Application.Responses;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ValidateTokenFunction
{
    private readonly IMediator _mediator;

    public ValidateTokenFunction(IMediator mediator) => _mediator = mediator;

    [Function("ValidateToken")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "surveys/{surveyId}/validate-token")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var body = await JsonSerializer.DeserializeAsync<ValidateTokenRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        if (body == null || string.IsNullOrEmpty(body.Token))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var result = await _mediator.Send(new ValidateTokenQuery(id, body.Token), ct);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { valid = result.Valid, error = result.Error }, ct);
        return response;
    }

    private class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
