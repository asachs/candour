namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Candour.Application.Surveys;

public class CloseSurveyHandlerTests
{
    private readonly Mock<ISurveyRepository> _repo = new();
    private readonly CloseSurveyHandler _handler;

    public CloseSurveyHandlerTests()
    {
        _handler = new CloseSurveyHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_SurveyNotFound_ReturnsFalse()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        // Act
        var result = await _handler.Handle(new CloseSurveyCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.False(result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SurveyExists_SetsClosedAndReturnsTrue()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Active };
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        // Act
        var result = await _handler.Handle(new CloseSurveyCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(SurveyStatus.Closed, survey.Status);
        _repo.Verify(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftSurvey_CanAlsoClose()
    {
        // Arrange - closing a Draft survey should also work (no status guard)
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Draft };
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        // Act
        var result = await _handler.Handle(new CloseSurveyCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(SurveyStatus.Closed, survey.Status);
    }
}
