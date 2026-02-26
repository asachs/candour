namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Candour.Application.Responses;

public class SubmitResponseHandlerTests
{
    private readonly Mock<ISurveyRepository> _surveyRepo = new();
    private readonly Mock<IResponseRepository> _responseRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ITimestampJitterService> _jitterService = new();
    private readonly SubmitResponseHandler _handler;

    public SubmitResponseHandlerTests()
    {
        _handler = new SubmitResponseHandler(
            _surveyRepo.Object,
            _responseRepo.Object,
            _tokenService.Object,
            _jitterService.Object);
    }

    [Fact]
    public async Task Handle_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var command = new SubmitResponseCommand(surveyId, "token", new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Survey not found", result.Error);
    }

    [Fact]
    public async Task Handle_SurveyNotActive_ReturnsFailure()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Draft };
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var command = new SubmitResponseCommand(surveyId, "token", new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Survey is not active", result.Error);
    }

    [Fact]
    public async Task Handle_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Active, BatchSecret = "secret" };
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _tokenService.Setup(t => t.ValidateToken("bad-token", "secret")).Returns(false);

        var command = new SubmitResponseCommand(surveyId, "bad-token", new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid token", result.Error);
    }

    [Fact]
    public async Task Handle_TokenAlreadyUsed_ReturnsFailure()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Active, BatchSecret = "secret" };
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _tokenService.Setup(t => t.ValidateToken("used-token", "secret")).Returns(true);
        _tokenService.Setup(t => t.HashToken("used-token")).Returns("hashed-used");
        _tokenService.Setup(t => t.IsTokenUsedAsync("hashed-used", surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SubmitResponseCommand(surveyId, "used-token", new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Token already used", result.Error);
    }

    [Fact]
    public async Task Handle_HappyPath_MarksTokenAndStoresResponse()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Status = SurveyStatus.Active,
            BatchSecret = "secret",
            TimestampJitterMinutes = 15
        };
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _tokenService.Setup(t => t.ValidateToken("valid-token", "secret")).Returns(true);
        _tokenService.Setup(t => t.HashToken("valid-token")).Returns("hashed-valid");
        _tokenService.Setup(t => t.IsTokenUsedAsync("hashed-valid", surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var jitteredTime = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _jitterService.Setup(j => j.ApplyJitter(It.IsAny<DateTime>(), 15))
            .Returns(jitteredTime);

        SurveyResponse? captured = null;
        _responseRepo.Setup(r => r.AddAsync(It.IsAny<SurveyResponse>(), It.IsAny<CancellationToken>()))
            .Callback<SurveyResponse, CancellationToken>((resp, _) => captured = resp)
            .ReturnsAsync((SurveyResponse r, CancellationToken _) => r);

        var answers = new Dictionary<string, string>
        {
            { "q1", "Yes" },
            { "q2", "Great team" }
        };
        var command = new SubmitResponseCommand(surveyId, "valid-token", answers);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);

        _tokenService.Verify(t => t.MarkTokenUsedAsync("hashed-valid", surveyId, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(captured);
        Assert.Equal(surveyId, captured!.SurveyId);
        Assert.Equal(jitteredTime, captured.SubmittedAt);
        Assert.Contains("q1", captured.Answers);
        Assert.Contains("Yes", captured.Answers);
    }

    [Fact]
    public async Task Handle_ClosedSurvey_ReturnsNotActive()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey { Id = surveyId, Status = SurveyStatus.Closed, BatchSecret = "secret" };
        _surveyRepo.Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var command = new SubmitResponseCommand(surveyId, "token", new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Survey is not active", result.Error);
    }
}
