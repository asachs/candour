namespace Candour.Application.Tests;

using System.Text.Json;
using Moq;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using Candour.Application.Responses;

public class GetAggregateResultsHandlerTests
{
    private readonly Mock<ISurveyRepository> _surveyRepo = new();
    private readonly Mock<IResponseRepository> _responseRepo = new();
    private readonly GetAggregateResultsHandler _handler;

    public GetAggregateResultsHandlerTests()
    {
        _handler = new GetAggregateResultsHandler(_surveyRepo.Object, _responseRepo.Object);
    }

    [Fact]
    public async Task Handle_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Survey not found", result.Error);
    }

    [Fact]
    public async Task Handle_BelowThreshold_ReturnsExactErrorMessage()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            AnonymityThreshold = 10,
            Questions = new List<Question>()
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Insufficient responses. Need 10, have 3.", result.Error);
    }

    [Fact]
    public async Task Handle_MultipleChoice_AggregatesOptionCountsAndPercentages()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            AnonymityThreshold = 2,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Type = QuestionType.MultipleChoice,
                    Text = "Pick one",
                    Options = JsonSerializer.Serialize(new List<string> { "A", "B", "C" }),
                    Order = 1
                }
            }
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _responseRepo.Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>
            {
                MakeResponse(surveyId, questionId, "A"),
                MakeResponse(surveyId, questionId, "A"),
                MakeResponse(surveyId, questionId, "B"),
                MakeResponse(surveyId, questionId, "C")
            });

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(surveyId, result.Data!.SurveyId);
        Assert.Equal("Test Survey", result.Data.SurveyTitle);
        Assert.Equal(4, result.Data.TotalResponses);

        var q = Assert.Single(result.Data.Questions);
        Assert.Equal("Pick one", q.QuestionText);
        Assert.Equal("MultipleChoice", q.QuestionType);
        Assert.Equal(2, q.OptionCounts["A"]);
        Assert.Equal(1, q.OptionCounts["B"]);
        Assert.Equal(1, q.OptionCounts["C"]);
        Assert.Equal(50.0, q.OptionPercentages["A"]);
        Assert.Equal(25.0, q.OptionPercentages["B"]);
        Assert.Equal(25.0, q.OptionPercentages["C"]);
    }

    [Fact]
    public async Task Handle_FreeText_CollectsAnswers()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "FreeText Survey",
            AnonymityThreshold = 2,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Type = QuestionType.FreeText,
                    Text = "Comments?",
                    Options = "[]",
                    Order = 1
                }
            }
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _responseRepo.Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>
            {
                MakeResponse(surveyId, questionId, "Great work"),
                MakeResponse(surveyId, questionId, "Needs improvement"),
                MakeResponse(surveyId, questionId, "Solid effort")
            });

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        var q = Assert.Single(result.Data!.Questions);
        Assert.Equal("FreeText", q.QuestionType);
        Assert.Equal(3, q.FreeTextAnswers.Count);
        // Answers are shuffled so we check containment, not order
        Assert.Contains("Great work", q.FreeTextAnswers);
        Assert.Contains("Needs improvement", q.FreeTextAnswers);
        Assert.Contains("Solid effort", q.FreeTextAnswers);
    }

    [Fact]
    public async Task Handle_Rating_CalculatesAverageAndCounts()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "Rating Survey",
            AnonymityThreshold = 2,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Type = QuestionType.Rating,
                    Text = "Rate 1-5",
                    Options = JsonSerializer.Serialize(new List<string> { "1", "2", "3", "4", "5" }),
                    Order = 1
                }
            }
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _responseRepo.Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>
            {
                MakeResponse(surveyId, questionId, "3"),
                MakeResponse(surveyId, questionId, "4"),
                MakeResponse(surveyId, questionId, "5"),
                MakeResponse(surveyId, questionId, "4")
            });

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        var q = Assert.Single(result.Data!.Questions);
        Assert.Equal("Rating", q.QuestionType);
        Assert.NotNull(q.AverageRating);
        Assert.Equal(4.0, q.AverageRating!.Value);
        Assert.Equal(1, q.OptionCounts["3"]);
        Assert.Equal(2, q.OptionCounts["4"]);
        Assert.Equal(1, q.OptionCounts["5"]);
    }

    [Fact]
    public async Task Handle_YesNo_AggregatesLikeMultipleChoice()
    {
        // Arrange
        var surveyId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            Title = "YesNo Survey",
            AnonymityThreshold = 2,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Type = QuestionType.YesNo,
                    Text = "Agree?",
                    Options = JsonSerializer.Serialize(new List<string> { "Yes", "No" }),
                    Order = 1
                }
            }
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _responseRepo.Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>
            {
                MakeResponse(surveyId, questionId, "Yes"),
                MakeResponse(surveyId, questionId, "Yes"),
                MakeResponse(surveyId, questionId, "No")
            });

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        var q = Assert.Single(result.Data!.Questions);
        Assert.Equal(2, q.OptionCounts["Yes"]);
        Assert.Equal(1, q.OptionCounts["No"]);
        double yesPercent = (2.0 / 3.0) * 100;
        Assert.Equal(yesPercent, q.OptionPercentages["Yes"], precision: 1);
    }

    [Fact]
    public async Task Handle_ExactlyAtThreshold_Succeeds()
    {
        // Arrange - threshold is 5, count is 5 (not below)
        var surveyId = Guid.NewGuid();
        var survey = new Survey
        {
            Id = surveyId,
            AnonymityThreshold = 5,
            Questions = new List<Question>()
        };
        _surveyRepo.Setup(r => r.GetWithQuestionsAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        _responseRepo.Setup(r => r.CountBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _responseRepo.Setup(r => r.GetBySurveyAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SurveyResponse>());

        // Act
        var result = await _handler.Handle(new GetAggregateResultsQuery(surveyId), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    // Helper to create a SurveyResponse with a single question answer
    private static SurveyResponse MakeResponse(Guid surveyId, Guid questionId, string answer)
    {
        var answers = new Dictionary<string, string> { { questionId.ToString(), answer } };
        return new SurveyResponse
        {
            SurveyId = surveyId,
            Answers = JsonSerializer.Serialize(answers),
            SubmittedAt = DateTime.UtcNow
        };
    }
}
