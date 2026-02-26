namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Candour.Application.Surveys;

public class CreateSurveyHandlerTests
{
    private readonly Mock<ISurveyRepository> _repo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly CreateSurveyHandler _handler;

    public CreateSurveyHandlerTests()
    {
        _handler = new CreateSurveyHandler(_repo.Object, _tokenService.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_SetsBatchSecretAndCreatesQuestions()
    {
        // Arrange
        var batchSecret = "test-batch-secret-base64";
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns(batchSecret);

        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var command = new CreateSurveyCommand(
            Title: "Team Feedback",
            Description: "Quarterly review",
            CreatorId: "creator-1",
            AnonymityThreshold: 10,
            TimestampJitterMinutes: 15,
            Questions: new List<CreateQuestionItem>
            {
                new(QuestionType.MultipleChoice, "How is morale?", new List<string> { "Good", "Bad" }, true, 1),
                new(QuestionType.FreeText, "Comments?", new List<string>(), false, 2)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(batchSecret, captured!.BatchSecret);
        Assert.Equal("Team Feedback", captured.Title);
        Assert.Equal("Quarterly review", captured.Description);
        Assert.Equal("creator-1", captured.CreatorId);
        Assert.Equal(10, captured.AnonymityThreshold);
        Assert.Equal(15, captured.TimestampJitterMinutes);
        Assert.Equal(2, captured.Questions.Count);

        var q1 = captured.Questions.First(q => q.Order == 1);
        Assert.Equal(QuestionType.MultipleChoice, q1.Type);
        Assert.Equal("How is morale?", q1.Text);
        Assert.Contains("Good", q1.Options);
        Assert.Contains("Bad", q1.Options);
        Assert.True(q1.Required);

        var q2 = captured.Questions.First(q => q.Order == 2);
        Assert.Equal(QuestionType.FreeText, q2.Type);
        Assert.False(q2.Required);

        _tokenService.Verify(t => t.GenerateBatchSecret(), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(-100, 5)]
    public async Task Handle_ZeroOrNegativeThreshold_DefaultsToFive(int input, int expected)
    {
        // Arrange
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns("secret");
        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var command = new CreateSurveyCommand("T", "D", "C", input, 5, new List<CreateQuestionItem>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(expected, captured!.AnonymityThreshold);
    }

    [Fact]
    public async Task Handle_PositiveThreshold_UsesProvidedValue()
    {
        // Arrange
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns("secret");
        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var command = new CreateSurveyCommand("T", "D", "C", 20, 5, new List<CreateQuestionItem>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(20, captured!.AnonymityThreshold);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(-50, 10)]
    public async Task Handle_NegativeJitter_DefaultsToTen(int input, int expected)
    {
        // Arrange
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns("secret");
        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var command = new CreateSurveyCommand("T", "D", "C", 5, input, new List<CreateQuestionItem>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expected, captured!.TimestampJitterMinutes);
    }

    [Fact]
    public async Task Handle_ZeroJitter_UsesZero()
    {
        // Arrange - zero is valid (>= 0)
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns("secret");
        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var command = new CreateSurveyCommand("T", "D", "C", 5, 0, new List<CreateQuestionItem>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, captured!.TimestampJitterMinutes);
    }

    [Fact]
    public async Task Handle_QuestionsWithOptions_SerializesToJson()
    {
        // Arrange
        _tokenService.Setup(t => t.GenerateBatchSecret()).Returns("secret");
        Survey? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => captured = s)
            .ReturnsAsync((Survey s, CancellationToken _) => s);

        var options = new List<string> { "Excellent", "Good", "Fair", "Poor" };
        var command = new CreateSurveyCommand("T", "D", "C", 5, 10,
            new List<CreateQuestionItem>
            {
                new(QuestionType.MultipleChoice, "Rate quality", options, true, 1)
            });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var question = Assert.Single(captured!.Questions);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<string>>(question.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(4, deserialized!.Count);
        Assert.Equal("Excellent", deserialized[0]);
        Assert.Equal("Poor", deserialized[3]);
    }
}
