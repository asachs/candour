namespace Candour.Functions.Integration.Tests;

using Candour.Application.Surveys;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Moq;

/// <summary>
/// Integration tests for survey query endpoints dispatched through real MediatR pipeline.
/// </summary>
public class SurveyEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public SurveyEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSurvey_ExistingSurvey_ReturnsSurveyWithQuestions()
    {
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            Status = SurveyStatus.Active,
            Questions = new List<Question>
            {
                new() { Text = "Q1", Type = QuestionType.MultipleChoice, Order = 1 },
                new() { Text = "Q2", Type = QuestionType.FreeText, Order = 2 },
                new() { Text = "Q3", Type = QuestionType.Rating, Order = 3 }
            }
        };

        _fixture.SurveyRepo
            .Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var result = await _fixture.Mediator.Send(new GetSurveyQuery(surveyId));

        Assert.NotNull(result);
        Assert.Equal("Test Survey", result!.Title);
        Assert.Equal(3, result.Questions.Count);
    }

    [Fact]
    public async Task GetSurvey_MissingSurvey_ReturnsNull()
    {
        var surveyId = Guid.NewGuid();

        _fixture.SurveyRepo
            .Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var result = await _fixture.Mediator.Send(new GetSurveyQuery(surveyId));

        Assert.Null(result);
    }

    [Fact]
    public async Task ListSurveys_ReturnsAllSurveys()
    {
        var surveys = new List<Survey>
        {
            new() { Title = "Survey 1" },
            new() { Title = "Survey 2" }
        };

        _fixture.SurveyRepo
            .Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(surveys);

        var result = await _fixture.Mediator.Send(new ListSurveysQuery());

        Assert.Equal(2, result.Count);
    }
}
