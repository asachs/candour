namespace Candour.Infrastructure.Tests;

using Candour.Core.Entities;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ResponseRepositoryTests
{
    private static CandourDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CandourDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CandourDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ReturnsSurveyResponseWithId()
    {
        using var ctx = CreateContext(nameof(AddAsync_ReturnsSurveyResponseWithId));
        var repo = new ResponseRepository(ctx);
        var response = new SurveyResponse
        {
            SurveyId = Guid.NewGuid(),
            Answers = "{\"q1\": \"yes\"}",
            SubmittedAt = DateTime.UtcNow
        };

        var result = await repo.AddAsync(response);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(response.SurveyId, result.SurveyId);
    }

    [Fact]
    public async Task GetByIdAsync_FindsAddedResponse()
    {
        using var ctx = CreateContext(nameof(GetByIdAsync_FindsAddedResponse));
        var repo = new ResponseRepository(ctx);
        var response = new SurveyResponse
        {
            SurveyId = Guid.NewGuid(),
            Answers = "{}",
            SubmittedAt = DateTime.UtcNow
        };
        await repo.AddAsync(response);

        var found = await repo.GetByIdAsync(response.Id);

        Assert.NotNull(found);
        Assert.Equal(response.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForMissing()
    {
        using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNullForMissing));
        var repo = new ResponseRepository(ctx);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllResponses()
    {
        using var ctx = CreateContext(nameof(ListAsync_ReturnsAllResponses));
        var repo = new ResponseRepository(ctx);
        var surveyId = Guid.NewGuid();
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyId, Answers = "{}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyId, Answers = "{}", SubmittedAt = DateTime.UtcNow });

        var list = await repo.ListAsync();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task CountBySurveyAsync_ReturnsCorrectCount()
    {
        using var ctx = CreateContext(nameof(CountBySurveyAsync_ReturnsCorrectCount));
        var repo = new ResponseRepository(ctx);
        var surveyA = Guid.NewGuid();
        var surveyB = Guid.NewGuid();
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyA, Answers = "{}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyA, Answers = "{}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyA, Answers = "{}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = surveyB, Answers = "{}", SubmittedAt = DateTime.UtcNow });

        var countA = await repo.CountBySurveyAsync(surveyA);
        var countB = await repo.CountBySurveyAsync(surveyB);
        var countNone = await repo.CountBySurveyAsync(Guid.NewGuid());

        Assert.Equal(3, countA);
        Assert.Equal(1, countB);
        Assert.Equal(0, countNone);
    }

    [Fact]
    public async Task GetBySurveyAsync_ReturnsOnlyMatchingResponses()
    {
        using var ctx = CreateContext(nameof(GetBySurveyAsync_ReturnsOnlyMatchingResponses));
        var repo = new ResponseRepository(ctx);
        var targetSurvey = Guid.NewGuid();
        var otherSurvey = Guid.NewGuid();
        await repo.AddAsync(new SurveyResponse { SurveyId = targetSurvey, Answers = "{\"match\":true}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = targetSurvey, Answers = "{\"match\":true}", SubmittedAt = DateTime.UtcNow });
        await repo.AddAsync(new SurveyResponse { SurveyId = otherSurvey, Answers = "{\"match\":false}", SubmittedAt = DateTime.UtcNow });

        var results = await repo.GetBySurveyAsync(targetSurvey);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(targetSurvey, r.SurveyId));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var ctx = CreateContext(nameof(UpdateAsync_PersistsChanges));
        var repo = new ResponseRepository(ctx);
        var response = new SurveyResponse { SurveyId = Guid.NewGuid(), Answers = "{\"old\":true}", SubmittedAt = DateTime.UtcNow };
        await repo.AddAsync(response);

        response.Answers = "{\"updated\":true}";
        await repo.UpdateAsync(response);

        var fetched = await repo.GetByIdAsync(response.Id);
        Assert.NotNull(fetched);
        Assert.Equal("{\"updated\":true}", fetched.Answers);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        using var ctx = CreateContext(nameof(DeleteAsync_RemovesEntity));
        var repo = new ResponseRepository(ctx);
        var response = new SurveyResponse { SurveyId = Guid.NewGuid(), Answers = "{}", SubmittedAt = DateTime.UtcNow };
        await repo.AddAsync(response);

        await repo.DeleteAsync(response);

        var fetched = await repo.GetByIdAsync(response.Id);
        Assert.Null(fetched);
    }
}
