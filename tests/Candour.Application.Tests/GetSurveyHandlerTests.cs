namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Application.Surveys;

public class GetSurveyHandlerTests
{
    private readonly Mock<ISurveyRepository> _repo = new();
    private readonly GetSurveyHandler _handler;

    public GetSurveyHandlerTests()
    {
        _handler = new GetSurveyHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_SurveyExists_ReturnsSurvey()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Existing Survey",
            Questions = new List<Question>
            {
                new() { Text = "Q1", Order = 1 }
            }
        };
        _repo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        // Act
        var result = await _handler.Handle(new GetSurveyQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(surveyId, result!.Id);
        Assert.Equal("Existing Survey", result.Title);
        Assert.Single(result.Questions);
        _repo.Verify(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SurveyNotFound_ReturnsNull()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _repo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        // Act
        var result = await _handler.Handle(new GetSurveyQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.Null(result);
        _repo.Verify(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
