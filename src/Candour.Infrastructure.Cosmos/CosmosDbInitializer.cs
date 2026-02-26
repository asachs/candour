namespace Candour.Infrastructure.Cosmos;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

public class CosmosDbInitializer
{
    private readonly CosmosClient _client;
    private readonly CosmosDbOptions _options;

    public CosmosDbInitializer(CosmosClient client, IOptions<CosmosDbOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var database = await _client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, cancellationToken: ct);

        // surveys container — partition key /id (each survey is its own partition)
        await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_options.SurveysContainer, "/id"), cancellationToken: ct);

        // responses container — partition key /surveyId (single-partition aggregation)
        await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_options.ResponsesContainer, "/surveyId"), cancellationToken: ct);

        // usedTokens container — partition key /surveyId, unique key on /tokenHash
        var usedTokensProperties = new ContainerProperties(_options.UsedTokensContainer, "/surveyId")
        {
            UniqueKeyPolicy = new UniqueKeyPolicy
            {
                UniqueKeys =
                {
                    new UniqueKey { Paths = { "/tokenHash" } }
                }
            }
        };
        await database.Database.CreateContainerIfNotExistsAsync(usedTokensProperties, cancellationToken: ct);
    }
}
