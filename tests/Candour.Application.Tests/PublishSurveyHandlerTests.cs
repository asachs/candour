namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Candour.Application.Surveys;

public class PublishSurveyHandlerTests
{
    private readonly Mock<ISurveyRepository> _repo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly PublishSurveyHandler _handler;

    public PublishSurveyHandlerTests()
    {
        _handler = new PublishSurveyHandler(_repo.Object, _tokenService.Object);
    }

    [Fact]
    public async Task Handle_SurveyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new PublishSurveyCommand(surveyId), CancellationToken.None));
        Assert.Equal("Survey not found", ex.Message);
    }

    [Fact]
    public async Task Handle_SurveyExists_SetsStatusToActive()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Draft, BatchSecret = "secret-key" };
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _tokenService.Setup(t => t.GenerateToken("secret-key")).Returns("token-1");

        // Act
        await _handler.Handle(new PublishSurveyCommand(surveyId, 5), CancellationToken.None);

        // Assert
        Assert.Equal(SurveyStatus.Active, survey.Status);
        _repo.Verify(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTokenCount_GeneratesCorrectNumberOfTokens()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Draft, BatchSecret = "secret-key" };
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        int callCount = 0;
        _tokenService.Setup(t => t.GenerateToken("secret-key"))
            .Returns(() => $"token-{++callCount}");

        // Act
        var result = await _handler.Handle(new PublishSurveyCommand(surveyId, 50), CancellationToken.None);

        // Assert
        Assert.Equal(surveyId, result.SurveyId);
        Assert.Equal(50, result.Tokens.Count);
        Assert.Equal("token-1", result.Tokens[0]);
        Assert.Equal("token-50", result.Tokens[49]);
        _tokenService.Verify(t => t.GenerateToken("secret-key"), Times.Exactly(50));
    }

    [Fact]
    public async Task Handle_DefaultTokenCount_Generates100Tokens()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Draft, BatchSecret = "secret" };
        _repo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _tokenService.Setup(t => t.GenerateToken("secret")).Returns("tok");

        // Act
        var result = await _handler.Handle(new PublishSurveyCommand(surveyId), CancellationToken.None);

        // Assert
        Assert.Equal(100, result.Tokens.Count);
        _tokenService.Verify(t => t.GenerateToken("secret"), Times.Exactly(100));
    }
}
