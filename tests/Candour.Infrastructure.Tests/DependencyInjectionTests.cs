namespace Candour.Infrastructure.Tests;

using Candour.Core.Interfaces;
using Candour.Infrastructure.AI;
using Candour.Infrastructure.Crypto;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
        };

        if (overrides != null)
            foreach (var kv in overrides)
                defaults[kv.Key] = kv.Value;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();
    }

    [Fact]
    public void AddInfrastructure_RegistersSurveyRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repo = scope.ServiceProvider.GetService<ISurveyRepository>();

        Assert.NotNull(repo);
        Assert.IsType<SurveyRepository>(repo);
    }

    [Fact]
    public void AddInfrastructure_RegistersResponseRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repo = scope.ServiceProvider.GetService<IResponseRepository>();

        Assert.NotNull(repo);
        Assert.IsType<ResponseRepository>(repo);
    }

    [Fact]
    public void AddInfrastructure_RegistersTokenService()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var svc = scope.ServiceProvider.GetService<ITokenService>();

        Assert.NotNull(svc);
        Assert.IsType<BlindTokenService>(svc);
    }

    [Fact]
    public void AddInfrastructure_RegistersTimestampJitterService()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        var svc = provider.GetService<ITimestampJitterService>();

        Assert.NotNull(svc);
        Assert.IsType<TimestampJitterService>(svc);
    }

    [Fact]
    public void AddInfrastructure_DefaultAiProvider_RegistersNullAnalyzer()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        var analyzer = provider.GetService<IAiAnalyzer>();

        Assert.NotNull(analyzer);
        Assert.IsType<NullAnalyzer>(analyzer);
    }

    [Fact]
    public void AddInfrastructure_OllamaProvider_RegistersOllamaAnalyzer()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Candour:AI:Provider"] = "ollama",
            ["Candour:AI:Endpoint"] = "http://localhost:11434",
            ["Candour:AI:Model"] = "test-model"
        });
        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        var analyzer = provider.GetService<IAiAnalyzer>();

        Assert.NotNull(analyzer);
        Assert.IsType<OllamaAnalyzer>(analyzer);
    }

    [Fact]
    public void AddInfrastructure_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<CandourDbContext>();

        Assert.NotNull(db);
    }

    [Fact]
    public void AddInfrastructure_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddInfrastructure(BuildConfig());

        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructure_RegistersBatchSecretProtector()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig());

        var provider = services.BuildServiceProvider();
        var protector = provider.GetService<IBatchSecretProtector>();

        Assert.NotNull(protector);
        Assert.IsType<DataProtectionBatchSecretProtector>(protector);
    }
}
