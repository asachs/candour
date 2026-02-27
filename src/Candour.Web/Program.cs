using Candour.Shared.Services;
using Candour.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Candour.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var useEntraId = builder.Configuration.GetValue<bool>("AzureAd:Enabled");

if (useEntraId)
{
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        var apiScope = builder.Configuration["AzureAd:ApiScope"];
        if (!string.IsNullOrEmpty(apiScope))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
        }
    });

    builder.Services.AddScoped(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(authorizedUrls: new[] { apiBaseUrl });

        handler.InnerHandler = new HttpClientHandler();
        return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
    });
}
else
{
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, DevAuthStateProvider>();
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
}

builder.Services.AddScoped<ICandourApiClient, CandourApiClient>();

// Public (unauthenticated) API client for anonymous respondent access.
// SurveyForm uses this to avoid AccessTokenNotAvailableException on public endpoints.
builder.Services.AddKeyedScoped<ICandourApiClient>("Public", (_, _) =>
    new CandourApiClient(new HttpClient { BaseAddress = new Uri(apiBaseUrl) }));

await builder.Build().RunAsync();
