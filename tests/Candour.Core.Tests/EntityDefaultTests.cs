using Candour.Core.Entities;
using Candour.Core.Enums;

namespace Candour.Core.Tests;

public class EntityDefaultTests
{
    // ── Survey Defaults ──────────────────────────────────────────────

    [Fact]
    public void Survey_Id_DefaultsToNonEmptyGuid()
    {
        var survey = new Survey();
        Assert.NotEqual(Guid.Empty, survey.Id);
    }

    [Fact]
    public void Survey_TwoInstances_HaveDistinctIds()
    {
        var a = new Survey();
        var b = new Survey();
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Survey_CreatorId_DefaultsToEmptyString()
    {
        var survey = new Survey();
        Assert.Equal(string.Empty, survey.CreatorId);
    }

    [Fact]
    public void Survey_Title_DefaultsToEmptyString()
    {
        var survey = new Survey();
        Assert.Equal(string.Empty, survey.Title);
    }

    [Fact]
    public void Survey_Description_DefaultsToEmptyString()
    {
        var survey = new Survey();
        Assert.Equal(string.Empty, survey.Description);
    }

    [Fact]
    public void Survey_Status_DefaultsToDraft()
    {
        var survey = new Survey();
        Assert.Equal(SurveyStatus.Draft, survey.Status);
    }

    [Fact]
    public void Survey_Settings_DefaultsToEmptyJsonObject()
    {
        var survey = new Survey();
        Assert.Equal("{}", survey.Settings);
    }

    [Fact]
    public void Survey_AnonymityThreshold_DefaultsToFive()
    {
        var survey = new Survey();
        Assert.Equal(5, survey.AnonymityThreshold);
    }

    [Fact]
    public void Survey_TimestampJitterMinutes_DefaultsToTen()
    {
        var survey = new Survey();
        Assert.Equal(10, survey.TimestampJitterMinutes);
    }

    [Fact]
    public void Survey_CreatedAt_DefaultsToApproximatelyUtcNow()
    {
        var before = DateTime.UtcNow;
        var survey = new Survey();
        var after = DateTime.UtcNow;

        Assert.InRange(survey.CreatedAt, before, after);
    }

    [Fact]
    public void Survey_BatchSecret_DefaultsToEmptyString()
    {
        var survey = new Survey();
        Assert.Equal(string.Empty, survey.BatchSecret);
    }

    [Fact]
    public void Survey_Questions_DefaultsToEmptyList()
    {
        var survey = new Survey();
        Assert.NotNull(survey.Questions);
        Assert.Empty(survey.Questions);
    }

    // ── Question Defaults ────────────────────────────────────────────

    [Fact]
    public void Question_Id_DefaultsToNonEmptyGuid()
    {
        var question = new Question();
        Assert.NotEqual(Guid.Empty, question.Id);
    }

    [Fact]
    public void Question_TwoInstances_HaveDistinctIds()
    {
        var a = new Question();
        var b = new Question();
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Question_SurveyId_DefaultsToEmptyGuid()
    {
        var question = new Question();
        Assert.Equal(Guid.Empty, question.SurveyId);
    }

    [Fact]
    public void Question_Type_DefaultsToMultipleChoice()
    {
        var question = new Question();
        Assert.Equal(QuestionType.MultipleChoice, question.Type);
    }

    [Fact]
    public void Question_Text_DefaultsToEmptyString()
    {
        var question = new Question();
        Assert.Equal(string.Empty, question.Text);
    }

    [Fact]
    public void Question_Options_DefaultsToEmptyJsonArray()
    {
        var question = new Question();
        Assert.Equal("[]", question.Options);
    }

    [Fact]
    public void Question_Required_DefaultsToTrue()
    {
        var question = new Question();
        Assert.True(question.Required);
    }

    [Fact]
    public void Question_Order_DefaultsToZero()
    {
        var question = new Question();
        Assert.Equal(0, question.Order);
    }

    [Fact]
    public void Question_Settings_DefaultsToEmptyJsonObject()
    {
        var question = new Question();
        Assert.Equal("{}", question.Settings);
    }

    [Fact]
    public void Question_Survey_DefaultsToNull()
    {
        var question = new Question();
        Assert.Null(question.Survey);
    }

    // ── SurveyResponse Defaults ──────────────────────────────────────

    [Fact]
    public void SurveyResponse_Id_DefaultsToNonEmptyGuid()
    {
        var response = new SurveyResponse();
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public void SurveyResponse_TwoInstances_HaveDistinctIds()
    {
        var a = new SurveyResponse();
        var b = new SurveyResponse();
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void SurveyResponse_SurveyId_DefaultsToEmptyGuid()
    {
        var response = new SurveyResponse();
        Assert.Equal(Guid.Empty, response.SurveyId);
    }

    [Fact]
    public void SurveyResponse_Answers_DefaultsToEmptyJsonObject()
    {
        var response = new SurveyResponse();
        Assert.Equal("{}", response.Answers);
    }

    [Fact]
    public void SurveyResponse_SubmittedAt_DefaultsToDateTimeDefault()
    {
        var response = new SurveyResponse();
        Assert.Equal(default(DateTime), response.SubmittedAt);
    }

    [Fact]
    public void SurveyResponse_HasNoIdentityFields()
    {
        // Architectural assertion: SurveyResponse must NOT contain identity-leaking properties.
        var properties = typeof(SurveyResponse).GetProperties();
        var propertyNames = properties.Select(p => p.Name).ToList();

        Assert.DoesNotContain("RespondentId", propertyNames);
        Assert.DoesNotContain("IpAddress", propertyNames);
        Assert.DoesNotContain("UserAgent", propertyNames);
        Assert.DoesNotContain("TokenReference", propertyNames);
    }

    // ── UsedToken Defaults ───────────────────────────────────────────

    [Fact]
    public void UsedToken_TokenHash_DefaultsToEmptyString()
    {
        var token = new UsedToken();
        Assert.Equal(string.Empty, token.TokenHash);
    }

    [Fact]
    public void UsedToken_SurveyId_DefaultsToEmptyGuid()
    {
        var token = new UsedToken();
        Assert.Equal(Guid.Empty, token.SurveyId);
    }

    [Fact]
    public void UsedToken_HasNoUsedAtProperty()
    {
        // UsedAt was removed to prevent timing-based de-anonymization (M1 fix)
        var properties = typeof(UsedToken).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name == "UsedAt");
    }
}
