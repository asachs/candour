namespace Candour.Application.Tests;

using Moq;
using MediatR;
using Candour.Core.Interfaces;
using Candour.Core.ValueObjects;
using Candour.Application.Analysis;
using Candour.Application.Responses;

public class RunAiAnalysisHandlerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IAiAnalyzer> _analyzer = new();
    private readonly RunAiAnalysisHandler _handler;

    public RunAiAnalysisHandlerTests()
    {
        _handler = new RunAiAnalysisHandler(_mediator.Object, _analyzer.Object);
    }

    [Fact]
    public async Task Handle_AggregateNotSuccessful_ReturnsNull()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<GetAggregateResultsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateResultResponse(false, Error: "Survey not found"));

        // Act
        var result = await _handler.Handle(new RunAiAnalysisCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.Null(result);
        _analyzer.Verify(a => a.AnalyzeAsync(It.IsAny<AggregateData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AggregateSuccessButNullData_ReturnsNull()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<GetAggregateResultsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateResultResponse(true, Data: null));

        // Act
        var result = await _handler.Handle(new RunAiAnalysisCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.Null(result);
        _analyzer.Verify(a => a.AnalyzeAsync(It.IsAny<AggregateData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AggregateSucceeds_ReturnsAnalysisReport()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var aggregateData = new AggregateData
        {
            SurveyId = surveyId,
            SurveyTitle = "Team Health",
            TotalResponses = 25,
            Questions = new List<QuestionAggregate>
            {
                new()
                {
                    QuestionText = "Morale?",
                    QuestionType = "MultipleChoice",
                    OptionCounts = new Dictionary<string, int> { { "High", 15 }, { "Low", 10 } }
                }
            }
        };

        _mediator.Setup(m => m.Send(It.IsAny<GetAggregateResultsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateResultResponse(true, aggregateData));

        var expectedReport = new AnalysisReport
        {
            SurveyId = surveyId,
            Summary = "Team morale is generally positive.",
            Themes = new List<string> { "Morale", "Engagement" },
            KeyInsights = new List<string> { "60% report high morale" },
            SentimentOverview = "Mostly positive"
        };

        _analyzer.Setup(a => a.AnalyzeAsync(aggregateData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _handler.Handle(new RunAiAnalysisCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(surveyId, result!.SurveyId);
        Assert.Equal("Team morale is generally positive.", result.Summary);
        Assert.Equal(2, result.Themes.Count);
        Assert.Single(result.KeyInsights);
        Assert.Equal("Mostly positive", result.SentimentOverview);

        _mediator.Verify(m => m.Send(
            It.Is<GetAggregateResultsQuery>(q => q.SurveyId == surveyId),
            It.IsAny<CancellationToken>()), Times.Once);
        _analyzer.Verify(a => a.AnalyzeAsync(aggregateData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BelowThreshold_ReturnsNull()
    {
        // Arrange - aggregate fails because of threshold
        var surveyId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<GetAggregateResultsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AggregateResultResponse(false, Error: "Insufficient responses. Need 10, have 3."));

        // Act
        var result = await _handler.Handle(new RunAiAnalysisCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.Null(result);
        _analyzer.Verify(a => a.AnalyzeAsync(It.IsAny<AggregateData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
