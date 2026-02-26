using Candour.Api.Middleware;
using Candour.Application;
using Candour.Infrastructure;
using FastEndpoints;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Destructure.ByTransforming<HttpContext>(ctx => new { Path = ctx.Request.Path.Value })
    .Filter.ByExcluding(e =>
        e.Properties.ContainsKey("RequestBody") ||
        e.Properties.ContainsKey("IpAddress") ||
        e.Properties.ContainsKey("UserAgent") ||
        e.Properties.ContainsKey("RemoteIpAddress"))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFastEndpoints();
builder.Services.AddOpenApi();

var app = builder.Build();

// Anonymity middleware FIRST -- before any handler can access IP
app.UseMiddleware<AnonymityMiddleware>();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "";
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

app.MapOpenApi();
app.MapScalarApiReference();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
