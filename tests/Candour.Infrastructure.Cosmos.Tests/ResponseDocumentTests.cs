namespace Candour.Infrastructure.Cosmos.Tests;

using Candour.Core.Entities;
using Candour.Infrastructure.Cosmos.Documents;

public class ResponseDocumentTests
{
    [Fact]
    public void FromEntity_MapsAllResponseFields()
    {
        var response = new SurveyResponse
        {
            Id = Guid.NewGuid(),
            SurveyId = Guid.NewGuid(),
            Answers = "{\"q1\":\"yes\"}",
            SubmittedAt = DateTime.UtcNow
        };

        var doc = ResponseDocument.FromEntity(response);

        Assert.Equal(response.Id.ToString(), doc.Id);
        Assert.Equal(response.SurveyId.ToString(), doc.SurveyId);
        Assert.Equal(response.Answers, doc.Answers);
        Assert.Equal(response.SubmittedAt, doc.SubmittedAt);
    }

    [Fact]
    public void ToEntity_RoundTripsCorrectly()
    {
        var original = new SurveyResponse
        {
            Id = Guid.NewGuid(),
            SurveyId = Guid.NewGuid(),
            Answers = "{\"q1\":\"no\"}",
            SubmittedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var doc = ResponseDocument.FromEntity(original);
        var entity = doc.ToEntity();

        Assert.Equal(original.Id, entity.Id);
        Assert.Equal(original.SurveyId, entity.SurveyId);
        Assert.Equal(original.Answers, entity.Answers);
        Assert.Equal(original.SubmittedAt, entity.SubmittedAt);
    }

    [Fact]
    public void ResponseDocument_PartitionKeyIsSurveyId()
    {
        var surveyId = Guid.NewGuid();
        var response = new SurveyResponse
        {
            SurveyId = surveyId,
            Answers = "{}",
            SubmittedAt = DateTime.UtcNow
        };

        var doc = ResponseDocument.FromEntity(response);

        Assert.Equal(surveyId.ToString(), doc.SurveyId);
    }
}
