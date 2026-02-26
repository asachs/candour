namespace Candour.Infrastructure.Cosmos;

using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Candour.Core.Interfaces;
using Candour.Infrastructure.Cosmos.Crypto;
using Candour.Infrastructure.Cosmos.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddCosmosInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Cosmos DB options
        services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.SectionName));

        // Cosmos client (singleton)
        var connectionString = configuration[$"{CosmosDbOptions.SectionName}:ConnectionString"]
            ?? throw new InvalidOperationException("CosmosDb:ConnectionString is required");

        services.AddSingleton(_ => new CosmosClient(connectionString, new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        }));

        // Database initializer
        services.AddSingleton<CosmosDbInitializer>();

        // Repositories
        services.AddSingleton<ISurveyRepository, CosmosSurveyRepository>();
        services.AddSingleton<IResponseRepository, CosmosResponseRepository>();
        services.AddSingleton<IUsedTokenRepository, CosmosUsedTokenRepository>();

        // Crypto — pure crypto services (no DB dependency)
        services.AddSingleton<ITokenService, Infrastructure.Crypto.BlindTokenService>();
        services.AddSingleton<ITimestampJitterService, Infrastructure.Crypto.TimestampJitterService>();

        // Batch secret protector — Key Vault or Data Protection fallback
        var keyVaultUri = configuration["KeyVault:Uri"];
        var keyName = configuration["KeyVault:KeyName"];

        if (!string.IsNullOrEmpty(keyVaultUri) && !string.IsNullOrEmpty(keyName))
        {
            // Azure Key Vault RSA wrap/unwrap
            services.AddSingleton<IBatchSecretProtector>(sp =>
            {
                var credential = new DefaultAzureCredential();
                var keyClient = new KeyClient(new Uri(keyVaultUri), credential);
                var key = keyClient.GetKey(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, credential);
                return new KeyVaultBatchSecretProtector(cryptoClient);
            });
        }
        else
        {
            // Local dev fallback — Data Protection
            services.AddDataProtection().SetApplicationName("Candour");
            services.AddSingleton<IBatchSecretProtector, Infrastructure.Crypto.DataProtectionBatchSecretProtector>();
        }

        // AI (default: disabled)
        var aiProvider = configuration.GetValue<string>("Candour:AI:Provider") ?? "None";
        switch (aiProvider.ToLowerInvariant())
        {
            case "ollama":
                var endpoint = configuration.GetValue<string>("Candour:AI:Endpoint") ?? "http://localhost:11434";
                var model = configuration.GetValue<string>("Candour:AI:Model") ?? "llama3";
                services.AddHttpClient<IAiAnalyzer, Infrastructure.AI.OllamaAnalyzer>(client =>
                    client.BaseAddress = new Uri(endpoint))
                    .ConfigureHttpClient((sp, client) => { })
                    .AddTypedClient<IAiAnalyzer>((client, sp) => new Infrastructure.AI.OllamaAnalyzer(client, model));
                break;
            default:
                services.AddSingleton<IAiAnalyzer, Infrastructure.AI.NullAnalyzer>();
                break;
        }

        return services;
    }
}
