namespace Candour.Infrastructure.Cosmos.Data;

using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosResponseRepository : IResponseRepository
{
    private readonly Container _container;

    public CosmosResponseRepository(CosmosClient client, IOptions<CosmosDbOptions> options)
    {
        var opts = options.Value;
        _container = client.GetContainer(opts.DatabaseName, opts.ResponsesContainer);
    }

    public async Task<SurveyResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Cross-partition query since we don't know the surveyId
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id.ToString());
        using var iterator = _container.GetItemQueryIterator<ResponseDocument>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            var doc = response.FirstOrDefault();
            return doc?.ToEntity();
        }
        return null;
    }

    public async Task<List<SurveyResponse>> ListAsync(CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var results = new List<SurveyResponse>();
        using var iterator = _container.GetItemQueryIterator<ResponseDocument>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            results.AddRange(response.Select(d => d.ToEntity()));
        }
        return results;
    }

    public async Task<SurveyResponse> AddAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        var doc = ResponseDocument.FromEntity(entity);
        var response = await _container.CreateItemAsync(doc, new PartitionKey(doc.SurveyId), cancellationToken: ct);
        return response.Resource.ToEntity();
    }

    public async Task UpdateAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        var doc = ResponseDocument.FromEntity(entity);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.SurveyId), cancellationToken: ct);
    }

    public async Task DeleteAsync(SurveyResponse entity, CancellationToken ct = default)
    {
        await _container.DeleteItemAsync<ResponseDocument>(
            entity.Id.ToString(), new PartitionKey(entity.SurveyId.ToString()), cancellationToken: ct);
    }

    public async Task<int> CountBySurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.surveyId = @surveyId")
            .WithParameter("@surveyId", surveyId.ToString());
        using var iterator = _container.GetItemQueryIterator<int>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(surveyId.ToString()) });
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            return response.FirstOrDefault();
        }
        return 0;
    }

    public async Task<List<SurveyResponse>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.surveyId = @surveyId")
            .WithParameter("@surveyId", surveyId.ToString());
        var results = new List<SurveyResponse>();
        using var iterator = _container.GetItemQueryIterator<ResponseDocument>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(surveyId.ToString()) });
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            results.AddRange(response.Select(d => d.ToEntity()));
        }
        return results;
    }
}
