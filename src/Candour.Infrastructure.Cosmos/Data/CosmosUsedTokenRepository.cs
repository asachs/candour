namespace Candour.Infrastructure.Cosmos.Data;

using Candour.Core.Interfaces;
using Candour.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosUsedTokenRepository : IUsedTokenRepository
{
    private readonly Container _container;

    public CosmosUsedTokenRepository(CosmosClient client, IOptions<CosmosDbOptions> options)
    {
        var opts = options.Value;
        _container = client.GetContainer(opts.DatabaseName, opts.UsedTokensContainer);
    }

    public async Task<bool> ExistsAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
    {
        try
        {
            var id = $"{surveyId}:{tokenHash}";
            await _container.ReadItemAsync<UsedTokenDocument>(
                id, new PartitionKey(surveyId.ToString()), cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task AddAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
    {
        var doc = UsedTokenDocument.Create(tokenHash, surveyId);
        try
        {
            await _container.CreateItemAsync(doc, new PartitionKey(doc.SurveyId), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Token already exists — unique key policy violation
            // This is expected for duplicate prevention
        }
    }
}
