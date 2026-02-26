namespace Candour.Infrastructure.Cosmos.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosSurveyRepository : ISurveyRepository
{
    private readonly Container _container;

    public CosmosSurveyRepository(CosmosClient client, IOptions<CosmosDbOptions> options)
    {
        var opts = options.Value;
        _container = client.GetContainer(opts.DatabaseName, opts.SurveysContainer);
    }

    public async Task<Survey?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<SurveyDocument>(
                id.ToString(), new PartitionKey(id.ToString()), cancellationToken: ct);
            return response.Resource.ToEntity();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Survey?> GetWithQuestionsAsync(Guid id, CancellationToken ct = default)
    {
        // Questions are embedded in the survey document, so same as GetByIdAsync
        return await GetByIdAsync(id, ct);
    }

    public async Task<List<Survey>> ListAsync(CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC");
        var results = new List<Survey>();
        using var iterator = _container.GetItemQueryIterator<SurveyDocument>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            results.AddRange(response.Select(d => d.ToEntity()));
        }
        return results;
    }

    public async Task<List<Survey>> GetByCreatorAsync(string creatorId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.creatorId = @creatorId ORDER BY c.createdAt DESC")
            .WithParameter("@creatorId", creatorId);
        var results = new List<Survey>();
        using var iterator = _container.GetItemQueryIterator<SurveyDocument>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            results.AddRange(response.Select(d => d.ToEntity()));
        }
        return results;
    }

    public async Task<Survey> AddAsync(Survey entity, CancellationToken ct = default)
    {
        var doc = SurveyDocument.FromEntity(entity);
        var response = await _container.CreateItemAsync(doc, new PartitionKey(doc.Id), cancellationToken: ct);
        return response.Resource.ToEntity();
    }

    public async Task UpdateAsync(Survey entity, CancellationToken ct = default)
    {
        var doc = SurveyDocument.FromEntity(entity);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Id), cancellationToken: ct);
    }

    public async Task DeleteAsync(Survey entity, CancellationToken ct = default)
    {
        await _container.DeleteItemAsync<SurveyDocument>(
            entity.Id.ToString(), new PartitionKey(entity.Id.ToString()), cancellationToken: ct);
    }
}
