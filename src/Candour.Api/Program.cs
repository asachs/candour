using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Candour.Api.Auth;
using Candour.Api.Middleware;
using Candour.Application;
using Candour.Infrastructure;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
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

// Authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);
builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("submit", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

// Anonymity middleware FIRST -- before any handler can access IP
app.UseMiddleware<AnonymityMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
