using Candour.Application;
using Candour.Functions.Auth;
using Candour.Functions.Middleware;
using Candour.Infrastructure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Auth configuration
builder.Services.Configure<EntraIdOptions>(
    builder.Configuration.GetSection("Candour:Auth"));

var useEntraId = builder.Configuration.GetValue<bool>("Candour:Auth:UseEntraId");
if (useEntraId)
{
    builder.Services.AddSingleton<IJwtTokenValidator, EntraIdJwtTokenValidator>();
}
else
{
    builder.Services.AddSingleton<IJwtTokenValidator, NoOpJwtTokenValidator>();
}

// Middleware: auth first, then anonymity
builder.UseMiddleware<AuthenticationMiddleware>();
builder.UseMiddleware<AnonymityMiddleware>();

// Application layer (MediatR handlers)
builder.Services.AddApplication();

// Cosmos DB infrastructure
builder.Services.AddCosmosInfrastructure(builder.Configuration);

var host = builder.Build();

// Initialize Cosmos DB containers on startup
using (var scope = host.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<CosmosDbInitializer>();
    await initializer.InitializeAsync();
}

host.Run();
