using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Candour.Infrastructure.Data;

namespace Candour.Api.Tests;

public class CandourApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Candour:ApiKey"] = "test-api-key-for-integration-tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL Entity Framework Core service descriptors to avoid dual-provider conflict.
            // EF registers many internal services (provider, query compilation, migrations, etc.)
            // that conflict when both Npgsql and InMemory are present.
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<CandourDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.Namespace != null && d.ServiceType.Namespace.Contains("EntityFrameworkCore")) ||
                    (d.ImplementationType?.Namespace != null && d.ImplementationType.Namespace.Contains("Npgsql")))
                .ToList();

            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            // Re-register only the InMemory database provider
            services.AddDbContext<CandourDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
