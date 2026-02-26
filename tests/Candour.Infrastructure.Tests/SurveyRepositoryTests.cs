namespace Candour.Infrastructure.Tests;

using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class SurveyRepositoryTests
{
    private static CandourDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CandourDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CandourDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ReturnsSurveyWithId()
    {
        using var ctx = CreateContext(nameof(AddAsync_ReturnsSurveyWithId));
        var repo = new SurveyRepository(ctx);
        var survey = new Survey { Title = "Test Survey", CreatorId = "user1" };

        var result = await repo.AddAsync(survey);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Survey", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_FindsAddedEntity()
    {
        using var ctx = CreateContext(nameof(GetByIdAsync_FindsAddedEntity));
        var repo = new SurveyRepository(ctx);
        var survey = new Survey { Title = "Findable", CreatorId = "user1" };
        await repo.AddAsync(survey);

        var found = await repo.GetByIdAsync(survey.Id);

        Assert.NotNull(found);
        Assert.Equal(survey.Id, found.Id);
        Assert.Equal("Findable", found.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForMissingId()
    {
        using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNullForMissingId));
        var repo = new SurveyRepository(ctx);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ReturnsOrderedByCreatedAtDescending()
    {
        using var ctx = CreateContext(nameof(ListAsync_ReturnsOrderedByCreatedAtDescending));
        var repo = new SurveyRepository(ctx);
        var oldest = new Survey { Title = "Oldest", CreatorId = "u", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var middle = new Survey { Title = "Middle", CreatorId = "u", CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) };
        var newest = new Survey { Title = "Newest", CreatorId = "u", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        await repo.AddAsync(oldest);
        await repo.AddAsync(middle);
        await repo.AddAsync(newest);

        var list = await repo.ListAsync();

        Assert.Equal(3, list.Count);
        Assert.Equal("Newest", list[0].Title);
        Assert.Equal("Middle", list[1].Title);
        Assert.Equal("Oldest", list[2].Title);
    }

    [Fact]
    public async Task GetWithQuestionsAsync_IncludesQuestions()
    {
        var dbName = nameof(GetWithQuestionsAsync_IncludesQuestions);
        var surveyId = Guid.NewGuid();

        // Seed data with one context, then query with a fresh one
        using (var seedCtx = CreateContext(dbName))
        {
            var survey = new Survey { Id = surveyId, Title = "With Questions", CreatorId = "u" };
            seedCtx.Surveys.Add(survey);
            seedCtx.Questions.AddRange(
                new Question { SurveyId = surveyId, Text = "Third", Order = 3, Type = QuestionType.FreeText },
                new Question { SurveyId = surveyId, Text = "First", Order = 1, Type = QuestionType.FreeText },
                new Question { SurveyId = surveyId, Text = "Second", Order = 2, Type = QuestionType.FreeText }
            );
            await seedCtx.SaveChangesAsync();
        }

        using var queryCtx = CreateContext(dbName);
        var repo = new SurveyRepository(queryCtx);
        var result = await repo.GetWithQuestionsAsync(surveyId);

        Assert.NotNull(result);
        Assert.Equal(3, result.Questions.Count);
        // Verify questions are included and ordered by Order
        var orders = result.Questions.Select(q => q.Order).ToList();
        Assert.Equal(new List<int> { 1, 2, 3 }, orders);
        Assert.Equal("First", result.Questions[0].Text);
        Assert.Equal("Second", result.Questions[1].Text);
        Assert.Equal("Third", result.Questions[2].Text);
    }

    [Fact]
    public async Task GetWithQuestionsAsync_ReturnsNullForMissingId()
    {
        using var ctx = CreateContext(nameof(GetWithQuestionsAsync_ReturnsNullForMissingId));
        var repo = new SurveyRepository(ctx);

        var result = await repo.GetWithQuestionsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCreatorAsync_FiltersAndOrdersByCreatedAtDescending()
    {
        using var ctx = CreateContext(nameof(GetByCreatorAsync_FiltersAndOrdersByCreatedAtDescending));
        var repo = new SurveyRepository(ctx);
        await repo.AddAsync(new Survey { Title = "Alice Old", CreatorId = "alice", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        await repo.AddAsync(new Survey { Title = "Alice New", CreatorId = "alice", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        await repo.AddAsync(new Survey { Title = "Bob Survey", CreatorId = "bob", CreatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc) });

        var aliceSurveys = await repo.GetByCreatorAsync("alice");

        Assert.Equal(2, aliceSurveys.Count);
        Assert.Equal("Alice New", aliceSurveys[0].Title);
        Assert.Equal("Alice Old", aliceSurveys[1].Title);
    }

    [Fact]
    public async Task GetByCreatorAsync_ReturnsEmptyForUnknownCreator()
    {
        using var ctx = CreateContext(nameof(GetByCreatorAsync_ReturnsEmptyForUnknownCreator));
        var repo = new SurveyRepository(ctx);
        await repo.AddAsync(new Survey { Title = "Exists", CreatorId = "someone" });

        var result = await repo.GetByCreatorAsync("nobody");

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var ctx = CreateContext(nameof(UpdateAsync_PersistsChanges));
        var repo = new SurveyRepository(ctx);
        var survey = new Survey { Title = "Original", CreatorId = "u" };
        await repo.AddAsync(survey);

        survey.Title = "Updated";
        await repo.UpdateAsync(survey);

        var fetched = await repo.GetByIdAsync(survey.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Updated", fetched.Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        using var ctx = CreateContext(nameof(DeleteAsync_RemovesEntity));
        var repo = new SurveyRepository(ctx);
        var survey = new Survey { Title = "To Delete", CreatorId = "u" };
        await repo.AddAsync(survey);

        await repo.DeleteAsync(survey);

        var fetched = await repo.GetByIdAsync(survey.Id);
        Assert.Null(fetched);
    }
}
