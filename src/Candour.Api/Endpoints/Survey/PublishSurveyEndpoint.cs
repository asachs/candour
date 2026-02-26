namespace Candour.Api.Endpoints.Survey;

using Candour.Application.Surveys;
using Candour.Shared.Contracts;
using FastEndpoints;
using MediatR;

public class PublishSurveyRequest
{
    public int TokenCount { get; set; } = 100;
}

public class PublishSurveyEndpoint : Endpoint<PublishSurveyRequest, SurveyLinkResponse>
{
    private readonly IMediator _mediator;

    public PublishSurveyEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("surveys/{SurveyId}/publish");
        Summary(s => s.Summary = "Publish a survey and generate tokens");
        Options(x => x.RequireRateLimiting("general"));
    }

    public override async Task HandleAsync(PublishSurveyRequest req, CancellationToken ct)
    {
        var surveyId = Route<Guid>("SurveyId");
        var result = await _mediator.Send(new PublishSurveyCommand(surveyId, req.TokenCount), ct);

        await HttpContext.Response.SendAsync(new SurveyLinkResponse
        {
            SurveyId = result.SurveyId,
            ShareableLink = $"/survey/{result.SurveyId}",
            Tokens = result.Tokens
        }, cancellation: ct);
    }
}
