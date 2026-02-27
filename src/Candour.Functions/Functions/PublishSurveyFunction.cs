namespace Candour.Functions.Functions;

using System.Text.Json;
using Candour.Application.Surveys;
using Candour.Shared.Contracts;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

public class PublishSurveyFunction
{
    private readonly IMediator _mediator;
    private readonly string? _frontendBaseUrl;

    public PublishSurveyFunction(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _frontendBaseUrl = configuration["Candour:FrontendBaseUrl"]?.TrimEnd('/');
    }

    [Function("PublishSurvey")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "surveys/{surveyId}/publish")] HttpRequestData req,
        string surveyId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(surveyId, out var id))
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var body = await JsonSerializer.DeserializeAsync<PublishRequestBody>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        var tokenCount = body?.TokenCount ?? 100;

        var result = await _mediator.Send(new PublishSurveyCommand(id, tokenCount), ct);

        var dto = new SurveyLinkResponse
        {
            SurveyId = result.SurveyId,
            ShareableLink = string.IsNullOrEmpty(_frontendBaseUrl)
                ? $"/survey/{result.SurveyId}"
                : $"{_frontendBaseUrl}/survey/{result.SurveyId}",
            Tokens = result.Tokens
        };

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto, ct);
        return response;
    }

    private class PublishRequestBody
    {
        public int TokenCount { get; set; } = 100;
    }
}
