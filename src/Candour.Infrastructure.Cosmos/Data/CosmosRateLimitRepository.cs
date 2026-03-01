namespace Candour.Infrastructure.Cosmos.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosRateLimitRepository : IRateLimitRepository
{
    private readonly Container _container;

    public CosmosRateLimitRepository(CosmosClient client, IOptions<CosmosDbOptions> options)
    {
        var opts = options.Value;
        _container = client.GetContainer(opts.DatabaseName, opts.RateLimitsContainer);
    }

    public async Task<RateLimitEntry?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<RateLimitDocument>(
                key, new PartitionKey(key), cancellationToken: ct);

            var doc = response.Resource;
            return new RateLimitEntry
            {
                Key = doc.Key,
                Count = doc.Count,
                WindowStart = doc.WindowStart,
                TtlSeconds = doc.Ttl
            };
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpsertAsync(RateLimitEntry entry, CancellationToken ct = default)
    {
        var doc = new RateLimitDocument
        {
            Id = entry.Key,
            Key = entry.Key,
            Count = entry.Count,
            WindowStart = entry.WindowStart,
            Ttl = entry.TtlSeconds
        };

        await _container.UpsertItemAsync(doc, new PartitionKey(doc.Key), cancellationToken: ct);
    }
}
