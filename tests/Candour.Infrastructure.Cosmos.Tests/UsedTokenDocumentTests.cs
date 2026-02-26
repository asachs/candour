namespace Candour.Infrastructure.Cosmos.Tests;

using Candour.Infrastructure.Cosmos.Documents;

public class UsedTokenDocumentTests
{
    [Fact]
    public void Create_SetsCompositeId()
    {
        var surveyId = Guid.NewGuid();
        var tokenHash = "abc123def456";

        var doc = UsedTokenDocument.Create(tokenHash, surveyId);

        Assert.Equal($"{surveyId}:{tokenHash}", doc.Id);
    }

    [Fact]
    public void Create_SetsPartitionKeyToSurveyId()
    {
        var surveyId = Guid.NewGuid();
        var tokenHash = "hash123";

        var doc = UsedTokenDocument.Create(tokenHash, surveyId);

        Assert.Equal(surveyId.ToString(), doc.SurveyId);
    }

    [Fact]
    public void Create_SetsTokenHash()
    {
        var surveyId = Guid.NewGuid();
        var tokenHash = "myhash";

        var doc = UsedTokenDocument.Create(tokenHash, surveyId);

        Assert.Equal(tokenHash, doc.TokenHash);
    }

    [Fact]
    public void Create_DifferentSurveysSameHash_DifferentIds()
    {
        var survey1 = Guid.NewGuid();
        var survey2 = Guid.NewGuid();
        var tokenHash = "samehash";

        var doc1 = UsedTokenDocument.Create(tokenHash, survey1);
        var doc2 = UsedTokenDocument.Create(tokenHash, survey2);

        Assert.NotEqual(doc1.Id, doc2.Id);
    }
}
