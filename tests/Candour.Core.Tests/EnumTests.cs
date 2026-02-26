using Candour.Core.Enums;

namespace Candour.Core.Tests;

public class EnumTests
{
    // ── QuestionType ─────────────────────────────────────────────────

    [Fact]
    public void QuestionType_HasExactlyFiveMembers()
    {
        var values = Enum.GetValues<QuestionType>();
        Assert.Equal(5, values.Length);
    }

    [Theory]
    [InlineData(QuestionType.MultipleChoice, 0)]
    [InlineData(QuestionType.FreeText, 1)]
    [InlineData(QuestionType.Rating, 2)]
    [InlineData(QuestionType.Matrix, 3)]
    [InlineData(QuestionType.YesNo, 4)]
    public void QuestionType_MemberHasExpectedIntValue(QuestionType member, int expected)
    {
        Assert.Equal(expected, (int)member);
    }

    [Theory]
    [InlineData("MultipleChoice")]
    [InlineData("FreeText")]
    [InlineData("Rating")]
    [InlineData("Matrix")]
    [InlineData("YesNo")]
    public void QuestionType_CanParseByName(string name)
    {
        Assert.True(Enum.TryParse<QuestionType>(name, out _));
    }

    [Fact]
    public void QuestionType_ContainsExpectedNames()
    {
        var names = Enum.GetNames<QuestionType>();
        Assert.Equal(
            new[] { "MultipleChoice", "FreeText", "Rating", "Matrix", "YesNo" },
            names);
    }

    // ── SurveyStatus ─────────────────────────────────────────────────

    [Fact]
    public void SurveyStatus_HasExactlyThreeMembers()
    {
        var values = Enum.GetValues<SurveyStatus>();
        Assert.Equal(3, values.Length);
    }

    [Theory]
    [InlineData(SurveyStatus.Draft, 0)]
    [InlineData(SurveyStatus.Active, 1)]
    [InlineData(SurveyStatus.Closed, 2)]
    public void SurveyStatus_MemberHasExpectedIntValue(SurveyStatus member, int expected)
    {
        Assert.Equal(expected, (int)member);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Active")]
    [InlineData("Closed")]
    public void SurveyStatus_CanParseByName(string name)
    {
        Assert.True(Enum.TryParse<SurveyStatus>(name, out _));
    }

    [Fact]
    public void SurveyStatus_ContainsExpectedNames()
    {
        var names = Enum.GetNames<SurveyStatus>();
        Assert.Equal(
            new[] { "Draft", "Active", "Closed" },
            names);
    }

    [Fact]
    public void SurveyStatus_DefaultEnumValue_IsDraft()
    {
        var defaultStatus = default(SurveyStatus);
        Assert.Equal(SurveyStatus.Draft, defaultStatus);
    }
}
