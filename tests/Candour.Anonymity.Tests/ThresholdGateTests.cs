namespace Candour.Anonymity.Tests;

using Candour.Application.Responses;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Moq;

public class ThresholdGateTests
{
    [Fact]
    public async Task GetResults_ReturnsForbidden_WhenBelowThreshold()
    {
        var surveyRepo = new Mock<ISurveyRepository>();
        var responseRepo = new Mock<IResponseRepository>();

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AnonymityThreshold = 5,
            Questions = new List<Question>()
        };

        surveyRepo.Setup(r => r.GetWithQuestionsAsync(survey.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        responseRepo.Setup(r => r.CountBySurveyAsync(survey.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // Below threshold of 5

        var handler = new GetAggregateResultsHandler(surveyRepo.Object, responseRepo.Object);
        var result = await handler.Handle(new GetAggregateResultsQuery(survey.Id), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Error);
    }

    [Fact]
    public async Task GetResults_ReturnsData_WhenAtThreshold()
    {
        var surveyRepo = new Mock<ISurveyRepository>();
        var responseRepo = new Mock<IResponseRepository>();

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AnonymityThreshold = 5,
            Questions = new List<Question>
            {
                new Question { Id = Guid.NewGuid(), Text = "Q1", Type = QuestionType.YesNo, Options = "[\"Yes\",\"No\"]", Order = 1 }
            }
        };

        surveyRepo.Setup(r => r.GetWithQuestionsAsync(survey.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        responseRepo.Setup(r => r.CountBySurveyAsync(survey.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // At threshold
        responseRepo.Setup(r => r.GetBySurveyAsync(survey.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>());

        var handler = new GetAggregateResultsHandler(surveyRepo.Object, responseRepo.Object);
        var result = await handler.Handle(new GetAggregateResultsQuery(survey.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
