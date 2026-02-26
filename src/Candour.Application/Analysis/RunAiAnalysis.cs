namespace Candour.Application.Analysis;

using Candour.Core.Interfaces;
using Candour.Core.ValueObjects;
using Candour.Application.Responses;
using MediatR;

public record RunAiAnalysisCommand(Guid SurveyId) : IRequest<AnalysisReport?>;

public class RunAiAnalysisHandler : IRequestHandler<RunAiAnalysisCommand, AnalysisReport?>
{
    private readonly IMediator _mediator;
    private readonly IAiAnalyzer _analyzer;

    public RunAiAnalysisHandler(IMediator mediator, IAiAnalyzer analyzer)
    {
        _mediator = mediator;
        _analyzer = analyzer;
    }

    public async Task<AnalysisReport?> Handle(RunAiAnalysisCommand request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAggregateResultsQuery(request.SurveyId), ct);
        if (!result.Success || result.Data == null)
            return null;

        // AI receives ONLY aggregate data -- never individual responses
        return await _analyzer.AnalyzeAsync(result.Data, ct);
    }
}
