namespace Candour.Infrastructure.Cosmos.Tests;

using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Infrastructure.Cosmos.Documents;

public class SurveyDocumentTests
{
    [Fact]
    public void FromEntity_MapsAllSurveyFields()
    {
        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Test Survey",
            Description = "A test",
            Status = SurveyStatus.Active,
            AnonymityThreshold = 10,
            TimestampJitterMinutes = 5,
            CreatedAt = DateTime.UtcNow,
            BatchSecret = "secret123",
            Questions = new List<Question>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Text = "Q1",
                    Type = QuestionType.MultipleChoice,
                    Options = "[\"A\",\"B\"]",
                    Required = true,
                    Order = 1
                }
            }
        };

        var doc = SurveyDocument.FromEntity(survey);

        Assert.Equal(survey.Id.ToString(), doc.Id);
        Assert.Equal(survey.Title, doc.Title);
        Assert.Equal(survey.Description, doc.Description);
        Assert.Equal("Active", doc.Status);
        Assert.Equal(survey.AnonymityThreshold, doc.AnonymityThreshold);
        Assert.Equal(survey.TimestampJitterMinutes, doc.TimestampJitterMinutes);
        Assert.Equal(survey.BatchSecret, doc.BatchSecret);
        Assert.Single(doc.Questions);
        Assert.Equal("Q1", doc.Questions[0].Text);
    }

    [Fact]
    public void ToEntity_RoundTripsCorrectly()
    {
        var original = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Round Trip",
            Description = "Test",
            Status = SurveyStatus.Draft,
            AnonymityThreshold = 5,
            TimestampJitterMinutes = 10,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            BatchSecret = "enc",
            Questions = new List<Question>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Text = "Favorite?",
                    Type = QuestionType.FreeText,
                    Options = "[]",
                    Required = false,
                    Order = 1
                }
            }
        };

        var doc = SurveyDocument.FromEntity(original);
        var entity = doc.ToEntity();

        Assert.Equal(original.Id, entity.Id);
        Assert.Equal(original.Title, entity.Title);
        Assert.Equal(original.Status, entity.Status);
        Assert.Equal(original.AnonymityThreshold, entity.AnonymityThreshold);
        Assert.Single(entity.Questions);
        Assert.Equal(original.Questions.First().Text, entity.Questions.First().Text);
    }

    [Fact]
    public void FromEntity_QuestionsPreserveOrder()
    {
        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Questions = new List<Question>
            {
                new() { Id = Guid.NewGuid(), Text = "Q1", Type = QuestionType.YesNo, Options = "[]", Order = 1 },
                new() { Id = Guid.NewGuid(), Text = "Q2", Type = QuestionType.Rating, Options = "[]", Order = 2 }
            }
        };

        var doc = SurveyDocument.FromEntity(survey);

        Assert.Equal(2, doc.Questions.Count);
        Assert.Equal("Q1", doc.Questions[0].Text);
        Assert.Equal("Q2", doc.Questions[1].Text);
    }
}
