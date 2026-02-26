namespace Candour.Functions.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// Tests the regex pattern used by AuthenticationMiddleware to determine
/// which routes require authentication (admin routes).
/// </summary>
public class AuthenticationMiddlewareRouteTests
{
    // The exact pattern from Candour.Functions.Middleware.AuthenticationMiddleware
    private static readonly Regex AdminRoutePattern = new(
        @"^/api/surveys(?:/[^/]+/(?:publish|analyze))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Theory]
    [InlineData("/api/surveys")]
    [InlineData("/api/surveys/abc-123/publish")]
    [InlineData("/api/surveys/some-guid/analyze")]
    public void Pattern_Matches_AdminRoutes(string path)
    {
        Assert.True(AdminRoutePattern.IsMatch(path),
            $"Expected admin route pattern to match: {path}");
    }

    [Theory]
    [InlineData("/api/surveys/some-guid")]
    [InlineData("/api/surveys/some-guid/responses")]
    [InlineData("/api/surveys/some-guid/results")]
    [InlineData("/api/surveys/some-guid/validate-token")]
    public void Pattern_DoesNotMatch_RespondentRoutes(string path)
    {
        Assert.False(AdminRoutePattern.IsMatch(path),
            $"Expected admin route pattern NOT to match respondent route: {path}");
    }

    [Theory]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/admin")]
    [InlineData("/api/surveys/some-guid/unknown")]
    [InlineData("/")]
    public void Pattern_DoesNotMatch_OtherRoutes(string path)
    {
        Assert.False(AdminRoutePattern.IsMatch(path),
            $"Expected admin route pattern NOT to match: {path}");
    }
}
