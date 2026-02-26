namespace Candour.Infrastructure;

using Candour.Core.Interfaces;
using Candour.Infrastructure.AI;
using Candour.Infrastructure.Crypto;
using Candour.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CandourDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();

        // Crypto
        services.AddScoped<ITokenService, BlindTokenService>();
        services.AddSingleton<ITimestampJitterService, TimestampJitterService>();

        // Data Protection for batch secret encryption at rest
        services.AddDataProtection();
        services.AddSingleton<IBatchSecretProtector, DataProtectionBatchSecretProtector>();

        // AI (default: disabled)
        var aiProvider = configuration.GetValue<string>("Candour:AI:Provider") ?? "None";
        switch (aiProvider.ToLowerInvariant())
        {
            case "ollama":
                var endpoint = configuration.GetValue<string>("Candour:AI:Endpoint") ?? "http://localhost:11434";
                var model = configuration.GetValue<string>("Candour:AI:Model") ?? "llama3";
                services.AddHttpClient<IAiAnalyzer, OllamaAnalyzer>(client =>
                    client.BaseAddress = new Uri(endpoint))
                    .ConfigureHttpClient((sp, client) => { })
                    .AddTypedClient<IAiAnalyzer>((client, sp) => new OllamaAnalyzer(client, model));
                break;
            default:
                services.AddSingleton<IAiAnalyzer, NullAnalyzer>();
                break;
        }

        return services;
    }
}
