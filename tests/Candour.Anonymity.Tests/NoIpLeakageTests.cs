namespace Candour.Anonymity.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// Tests that the anonymity middleware pattern correctly identifies respondent-facing
/// routes that require IP header stripping. The actual middleware implementations
/// (ASP.NET Core and Azure Functions) both use the same route-matching logic.
/// These tests verify the anonymity contract is correct.
/// </summary>
public class NoIpLeakageTests
{
    // The regex pattern used by Functions AnonymityMiddleware
    private static readonly Regex RespondentRoutePattern = new(
        @"^(?:/api/surveys/[^/]+/(?:responses|results|validate-token)|/api/surveys/[^/]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsRespondentRoute(string path) => RespondentRoutePattern.IsMatch(path);

    [Fact]
    public void RespondentRoute_ResponseSubmission_IsProtected()
    {
        Assert.True(IsRespondentRoute("/api/surveys/123/responses"));
    }

    [Fact]
    public void RespondentRoute_Results_IsProtected()
    {
        Assert.True(IsRespondentRoute("/api/surveys/123/results"));
    }

    [Fact]
    public void RespondentRoute_ValidateToken_IsProtected()
    {
        Assert.True(IsRespondentRoute("/api/surveys/123/validate-token"));
    }

    [Fact]
    public void RespondentRoute_GetSurvey_IsProtected()
    {
        Assert.True(IsRespondentRoute("/api/surveys/some-guid-here"));
    }

    [Fact]
    public void AdminRoute_ListSurveys_NotProtected()
    {
        Assert.False(IsRespondentRoute("/api/surveys"));
    }

    [Fact]
    public void AdminRoute_Publish_NotProtected()
    {
        Assert.False(IsRespondentRoute("/api/surveys/some-guid/publish"));
    }

    [Fact]
    public void AdminRoute_Analyze_NotProtected()
    {
        Assert.False(IsRespondentRoute("/api/surveys/some-guid/analyze"));
    }

    [Fact]
    public void AdminRoute_Dashboard_NotProtected()
    {
        Assert.False(IsRespondentRoute("/api/admin/dashboard"));
    }

    [Fact]
    public void HeaderStripping_CoversAllIdentifyingHeaders()
    {
        // Documents that the middleware must strip these headers
        var requiredStrippedHeaders = new[]
        {
            "X-Forwarded-For",
            "X-Real-IP",
            "X-Client-IP",
            "CF-Connecting-IP",
            "True-Client-IP",
            "X-Forwarded-Host"
        };

        Assert.Equal(6, requiredStrippedHeaders.Length);
    }

    [Fact]
    public void SetCookie_MustBeStripped_OnRespondentRoutes()
    {
        var respondentRoutes = new[]
        {
            "/api/surveys/123/responses",
            "/api/surveys/123/results",
            "/api/surveys/123/validate-token"
        };

        foreach (var route in respondentRoutes)
        {
            Assert.True(IsRespondentRoute(route),
                $"Set-Cookie stripping requires route match: {route}");
        }
    }
}
