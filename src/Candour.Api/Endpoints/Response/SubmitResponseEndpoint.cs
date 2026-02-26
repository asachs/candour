namespace Candour.Api.Endpoints.Response;

using Candour.Application.Responses;
using Candour.Shared.Contracts;
using FastEndpoints;
using MediatR;

public class SubmitResponseEndpoint : Endpoint<SubmitResponseRequest>
{
    private readonly IMediator _mediator;

    public SubmitResponseEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("surveys/{SurveyId}/responses");
        AllowAnonymous(); // Respondents never authenticate
        Summary(s => s.Summary = "Submit an anonymous survey response");
    }

    public override async Task HandleAsync(SubmitResponseRequest req, CancellationToken ct)
    {
        var surveyId = Route<Guid>("SurveyId");
        var result = await _mediator.Send(new SubmitResponseCommand(surveyId, req.Token, req.Answers), ct);

        if (result.Success)
            await HttpContext.Response.SendOkAsync(ct);
        else
            await HttpContext.Response.SendAsync(new { error = result.Error }, 400, cancellation: ct);
    }
}
