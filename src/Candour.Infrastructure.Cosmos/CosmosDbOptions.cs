namespace Candour.Infrastructure.Cosmos;

public class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "candour";
    public string SurveysContainer { get; set; } = "surveys";
    public string ResponsesContainer { get; set; } = "responses";
    public string UsedTokensContainer { get; set; } = "usedTokens";
}
