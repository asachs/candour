using Candour.Core.ValueObjects;

namespace Candour.Core.Tests;

public class ValueObjectTests
{
    // ── AggregateData Defaults ───────────────────────────────────────

    [Fact]
    public void AggregateData_SurveyId_DefaultsToEmptyGuid()
    {
        var data = new AggregateData();
        Assert.Equal(Guid.Empty, data.SurveyId);
    }

    [Fact]
    public void AggregateData_SurveyTitle_DefaultsToEmptyString()
    {
        var data = new AggregateData();
        Assert.Equal(string.Empty, data.SurveyTitle);
    }

    [Fact]
    public void AggregateData_TotalResponses_DefaultsToZero()
    {
        var data = new AggregateData();
        Assert.Equal(0, data.TotalResponses);
    }

    [Fact]
    public void AggregateData_Questions_DefaultsToEmptyNonNullList()
    {
        var data = new AggregateData();
        Assert.NotNull(data.Questions);
        Assert.Empty(data.Questions);
    }

    [Fact]
    public void AggregateData_Questions_IsModifiable()
    {
        var data = new AggregateData();
        data.Questions.Add(new QuestionAggregate { QuestionText = "Test" });
        Assert.Single(data.Questions);
    }

    // ── QuestionAggregate Defaults ───────────────────────────────────

    [Fact]
    public void QuestionAggregate_QuestionText_DefaultsToEmptyString()
    {
        var qa = new QuestionAggregate();
        Assert.Equal(string.Empty, qa.QuestionText);
    }

    [Fact]
    public void QuestionAggregate_QuestionType_DefaultsToEmptyString()
    {
        var qa = new QuestionAggregate();
        Assert.Equal(string.Empty, qa.QuestionType);
    }

    [Fact]
    public void QuestionAggregate_OptionCounts_DefaultsToEmptyNonNullDictionary()
    {
        var qa = new QuestionAggregate();
        Assert.NotNull(qa.OptionCounts);
        Assert.Empty(qa.OptionCounts);
    }

    [Fact]
    public void QuestionAggregate_OptionPercentages_DefaultsToEmptyNonNullDictionary()
    {
        var qa = new QuestionAggregate();
        Assert.NotNull(qa.OptionPercentages);
        Assert.Empty(qa.OptionPercentages);
    }

    [Fact]
    public void QuestionAggregate_FreeTextAnswers_DefaultsToEmptyNonNullList()
    {
        var qa = new QuestionAggregate();
        Assert.NotNull(qa.FreeTextAnswers);
        Assert.Empty(qa.FreeTextAnswers);
    }

    [Fact]
    public void QuestionAggregate_AverageRating_DefaultsToNull()
    {
        var qa = new QuestionAggregate();
        Assert.Null(qa.AverageRating);
    }

    [Fact]
    public void QuestionAggregate_OptionCounts_IsModifiable()
    {
        var qa = new QuestionAggregate();
        qa.OptionCounts["Yes"] = 42;
        Assert.Equal(42, qa.OptionCounts["Yes"]);
    }

    [Fact]
    public void QuestionAggregate_OptionPercentages_IsModifiable()
    {
        var qa = new QuestionAggregate();
        qa.OptionPercentages["Yes"] = 75.5;
        Assert.Equal(75.5, qa.OptionPercentages["Yes"]);
    }

    [Fact]
    public void QuestionAggregate_FreeTextAnswers_IsModifiable()
    {
        var qa = new QuestionAggregate();
        qa.FreeTextAnswers.Add("Great survey");
        Assert.Single(qa.FreeTextAnswers);
        Assert.Equal("Great survey", qa.FreeTextAnswers[0]);
    }

    // ── AnalysisReport Defaults ──────────────────────────────────────

    [Fact]
    public void AnalysisReport_SurveyId_DefaultsToEmptyGuid()
    {
        var report = new AnalysisReport();
        Assert.Equal(Guid.Empty, report.SurveyId);
    }

    [Fact]
    public void AnalysisReport_Summary_DefaultsToEmptyString()
    {
        var report = new AnalysisReport();
        Assert.Equal(string.Empty, report.Summary);
    }

    [Fact]
    public void AnalysisReport_Themes_DefaultsToEmptyNonNullList()
    {
        var report = new AnalysisReport();
        Assert.NotNull(report.Themes);
        Assert.Empty(report.Themes);
    }

    [Fact]
    public void AnalysisReport_KeyInsights_DefaultsToEmptyNonNullList()
    {
        var report = new AnalysisReport();
        Assert.NotNull(report.KeyInsights);
        Assert.Empty(report.KeyInsights);
    }

    [Fact]
    public void AnalysisReport_SentimentOverview_DefaultsToEmptyString()
    {
        var report = new AnalysisReport();
        Assert.Equal(string.Empty, report.SentimentOverview);
    }

    [Fact]
    public void AnalysisReport_GeneratedAt_DefaultsToApproximatelyUtcNow()
    {
        var before = DateTime.UtcNow;
        var report = new AnalysisReport();
        var after = DateTime.UtcNow;

        Assert.InRange(report.GeneratedAt, before, after);
    }

    [Fact]
    public void AnalysisReport_Themes_IsModifiable()
    {
        var report = new AnalysisReport();
        report.Themes.Add("Employee satisfaction");
        report.Themes.Add("Work-life balance");
        Assert.Equal(2, report.Themes.Count);
    }

    [Fact]
    public void AnalysisReport_KeyInsights_IsModifiable()
    {
        var report = new AnalysisReport();
        report.KeyInsights.Add("Morale is high");
        Assert.Single(report.KeyInsights);
    }
}
