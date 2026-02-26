namespace Candour.Api.Endpoints.Survey;

using Candour.Application.Surveys;
using Candour.Core.Enums;
using Candour.Shared.Contracts;
using Candour.Shared.Models;
using FastEndpoints;
using MediatR;
using System.Text.Json;

public class CreateSurveyEndpoint : Endpoint<CreateSurveyRequest, SurveyDto>
{
    private readonly IMediator _mediator;

    public CreateSurveyEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("surveys");
        AllowAnonymous(); // TODO: Require auth for creators
        Summary(s => s.Summary = "Create a new survey");
    }

    public override async Task HandleAsync(CreateSurveyRequest req, CancellationToken ct)
    {
        var command = new CreateSurveyCommand(
            req.Title,
            req.Description,
            "anonymous-creator", // TODO: Get from auth context
            req.AnonymityThreshold,
            req.TimestampJitterMinutes,
            req.Questions.Select(q => new CreateQuestionItem(
                Enum.Parse<QuestionType>(q.Type, ignoreCase: true),
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

        await HttpContext.Response.SendCreatedAtAsync($"/api/surveys/{dto.Id}", dto.Id, dto, cancellation: ct);
    }
}
