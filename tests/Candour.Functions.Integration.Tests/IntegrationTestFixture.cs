namespace Candour.Functions.Integration.Tests;

using Candour.Application;
using Candour.Core.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// Shared fixture that builds a DI container with real MediatR pipeline
/// and mocked repository/service dependencies. Validates that DI wiring
/// is correct and handlers dispatch through the full MediatR pipeline.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public IMediator Mediator { get; }
    public Mock<ISurveyRepository> SurveyRepo { get; } = new();
    public Mock<IResponseRepository> ResponseRepo { get; } = new();
    public Mock<IUsedTokenRepository> UsedTokenRepo { get; } = new();
    public Mock<ITokenService> TokenService { get; } = new();
    public Mock<ITimestampJitterService> JitterService { get; } = new();
    public Mock<IBatchSecretProtector> Protector { get; } = new();
    public Mock<IAiAnalyzer> AiAnalyzer { get; } = new();
    public Mock<IRateLimitRepository> RateLimitRepo { get; } = new();

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // MediatR 14 licensing requires ILoggerFactory and IConfiguration
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Real MediatR pipeline (assembly-scanned handlers)
        services.AddApplication();

        // Mocked infrastructure
        services.AddSingleton(SurveyRepo.Object);
        services.AddSingleton(ResponseRepo.Object);
        services.AddSingleton(UsedTokenRepo.Object);
        services.AddSingleton(TokenService.Object);
        services.AddSingleton(JitterService.Object);
        services.AddSingleton(Protector.Object);
        services.AddSingleton(AiAnalyzer.Object);
        services.AddSingleton(RateLimitRepo.Object);

        var provider = services.BuildServiceProvider();
        Mediator = provider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
