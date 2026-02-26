namespace Candour.Functions.Functions;

using System.Security.Claims;
using System.Text.Json;
using Candour.Application.Surveys;
using Candour.Core.Enums;
using Candour.Shared.Contracts;
using Candour.Shared.Models;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class CreateSurveyFunction
{
    private readonly IMediator _mediator;

    public CreateSurveyFunction(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("CreateSurvey")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "surveys")] HttpRequestData req,
        FunctionContext context,
        CancellationToken ct)
    {
        var body = await JsonSerializer.DeserializeAsync<CreateSurveyRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        if (body == null)
        {
            var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            return badReq;
        }

        var creatorId = "dev-user";
        if (context.Items.TryGetValue("User", out var userObj) && userObj is ClaimsPrincipal principal)
        {
            creatorId = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                        ?? principal.FindFirst("oid")?.Value
                        ?? "dev-user";
        }

        var command = new CreateSurveyCommand(
            body.Title,
            body.Description,
            creatorId,
            body.AnonymityThreshold,
            body.TimestampJitterMinutes,
            body.Questions.Select(q => new CreateQuestionItem(
                Enum.TryParse<QuestionType>(q.Type, ignoreCase: true, out var parsedType) ? parsedType : QuestionType.FreeText,
                q.Text,
                q.Options,
                q.Required,
                q.Order
            )).ToList()
        );

        var survey = await _mediator.Send(command, ct);

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

        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(dto, ct);
        return response;
    }
}
