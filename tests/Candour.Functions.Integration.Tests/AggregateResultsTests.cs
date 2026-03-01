namespace Candour.Functions.Integration.Tests;

using System.Text.Json;
using Candour.Application.Responses;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Moq;

/// <summary>
/// Integration tests for aggregate results endpoint including threshold gating,
/// dispatched through real MediatR pipeline.
/// </summary>
public class AggregateResultsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AggregateResultsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAggregateResults_BelowThreshold_ReturnsGateError()
    {
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Gated Survey",
            Status = SurveyStatus.Active,
            AnonymityThreshold = 5,
            Questions = new List<Question>()
        };

        _fixture.SurveyRepo
            .Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        _fixture.ResponseRepo
            .Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _fixture.Mediator.Send(new GetAggregateResultsQuery(surveyId));

        Assert.False(result.Success);
        Assert.Contains("Insufficient responses", result.Error);
        Assert.Contains("Need 5", result.Error);
        Assert.Contains("have 2", result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAggregateResults_AboveThreshold_ReturnsAggregatedData()
    {
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Full Results Survey",
            Status = SurveyStatus.Active,
            AnonymityThreshold = 2,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Text = "Favorite color?",
                    Type = QuestionType.MultipleChoice,
                    Options = JsonSerializer.Serialize(new[] { "Red", "Blue", "Green" }),
                    Order = 1
                }
            }
        };

        var responses = new List<SurveyResponse>
        {
            new() { SurveyId = surveyId, Answers = JsonSerializer.Serialize(new Dictionary<string, string> { [questionId.ToString()] = "Red" }) },
            new() { SurveyId = surveyId, Answers = JsonSerializer.Serialize(new Dictionary<string, string> { [questionId.ToString()] = "Blue" }) },
            new() { SurveyId = surveyId, Answers = JsonSerializer.Serialize(new Dictionary<string, string> { [questionId.ToString()] = "Red" }) }
        };

        _fixture.SurveyRepo
            .Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        _fixture.ResponseRepo
            .Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _fixture.ResponseRepo
            .Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        var result = await _fixture.Mediator.Send(new GetAggregateResultsQuery(surveyId));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data!.TotalResponses);
        Assert.Single(result.Data.Questions);

        var q = result.Data.Questions[0];
        Assert.Equal("Favorite color?", q.QuestionText);
        Assert.Equal(2, q.OptionCounts["Red"]);
        Assert.Equal(1, q.OptionCounts["Blue"]);
        Assert.Equal(0, q.OptionCounts["Green"]);
    }
}
