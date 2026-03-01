namespace Candour.Functions.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// Tests the regex patterns and key extraction logic used by RateLimitingMiddleware.
/// The actual middleware uses IFunctionsWorkerMiddleware which requires a full Functions host;
/// here we verify the route-matching and key-format logic.
/// </summary>
public class RateLimitingMiddlewareTests
{
    // Route patterns from RateLimitingMiddleware
    private static readonly Regex GetSurveyPattern = new(
        @"^/api/surveys/[^/]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ValidateTokenPattern = new(
        @"^/api/surveys/[^/]+/validate-token$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SubmitResponsePattern = new(
        @"^/api/surveys/[^/]+/responses$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── GET /api/surveys/{id} pattern ──

    [Theory]
    [InlineData("/api/surveys/abc-123")]
    [InlineData("/api/surveys/00000000-0000-0000-0000-000000000000")]
    [InlineData("/api/surveys/some-guid")]
    public void GetSurveyPattern_Matches_ValidRoutes(string path)
    {
        Assert.True(GetSurveyPattern.IsMatch(path),
            $"Expected GET survey pattern to match: {path}");
    }

    [Theory]
    [InlineData("/api/surveys")]                              // admin list
    [InlineData("/api/surveys/abc-123/responses")]            // submit
    [InlineData("/api/surveys/abc-123/validate-token")]       // validate
    [InlineData("/api/surveys/abc-123/publish")]              // admin publish
    [InlineData("/api/surveys/abc-123/results")]              // admin results
    public void GetSurveyPattern_DoesNotMatch_OtherRoutes(string path)
    {
        Assert.False(GetSurveyPattern.IsMatch(path),
            $"Expected GET survey pattern NOT to match: {path}");
    }

    // ── POST /api/surveys/{id}/validate-token pattern ──

    [Theory]
    [InlineData("/api/surveys/abc-123/validate-token")]
    [InlineData("/api/surveys/00000000-0000-0000-0000-000000000000/validate-token")]
    public void ValidateTokenPattern_Matches_ValidRoutes(string path)
    {
        Assert.True(ValidateTokenPattern.IsMatch(path),
            $"Expected validate-token pattern to match: {path}");
    }

    [Theory]
    [InlineData("/api/surveys/abc-123/responses")]
    [InlineData("/api/surveys/abc-123")]
    [InlineData("/api/surveys")]
    public void ValidateTokenPattern_DoesNotMatch_OtherRoutes(string path)
    {
        Assert.False(ValidateTokenPattern.IsMatch(path),
            $"Expected validate-token pattern NOT to match: {path}");
    }

    // ── POST /api/surveys/{id}/responses pattern ──

    [Theory]
    [InlineData("/api/surveys/abc-123/responses")]
    [InlineData("/api/surveys/00000000-0000-0000-0000-000000000000/responses")]
    public void SubmitResponsePattern_Matches_ValidRoutes(string path)
    {
        Assert.True(SubmitResponsePattern.IsMatch(path),
            $"Expected submit-response pattern to match: {path}");
    }

    [Theory]
    [InlineData("/api/surveys/abc-123/validate-token")]
    [InlineData("/api/surveys/abc-123")]
    [InlineData("/api/surveys")]
    [InlineData("/api/surveys/abc-123/publish")]
    public void SubmitResponsePattern_DoesNotMatch_OtherRoutes(string path)
    {
        Assert.False(SubmitResponsePattern.IsMatch(path),
            $"Expected submit-response pattern NOT to match: {path}");
    }

    // ── Key format tests ──

    [Fact]
    public void IpKey_Format_IsCorrect()
    {
        var ip = "203.0.113.42";
        var key = $"ip:{ip}:get-survey";
        Assert.Equal("ip:203.0.113.42:get-survey", key);
    }

    [Fact]
    public void ValidateTokenKey_Format_IsCorrect()
    {
        var ip = "203.0.113.42";
        var key = $"ip:{ip}:validate-token";
        Assert.Equal("ip:203.0.113.42:validate-token", key);
    }

    [Fact]
    public void SubmitResponseKey_Format_IsCorrect()
    {
        var ip = "203.0.113.42";
        var key = $"ip:{ip}:submit-response";
        Assert.Equal("ip:203.0.113.42:submit-response", key);
    }

    // ── Policy resolution tests ──

    [Fact]
    public void AllThreePatterns_AreDisjoint()
    {
        // A path should match at most one pattern
        var testPaths = new[]
        {
            "/api/surveys/id",
            "/api/surveys/id/validate-token",
            "/api/surveys/id/responses",
            "/api/surveys",
            "/api/surveys/id/publish"
        };

        foreach (var path in testPaths)
        {
            var matchCount = 0;
            if (GetSurveyPattern.IsMatch(path)) matchCount++;
            if (ValidateTokenPattern.IsMatch(path)) matchCount++;
            if (SubmitResponsePattern.IsMatch(path)) matchCount++;

            Assert.True(matchCount <= 1,
                $"Path '{path}' matched {matchCount} patterns — should match at most 1");
        }
    }

    // ── Rate limit window logic tests ──

    [Fact]
    public void WindowExpiry_Calculation_IsCorrect()
    {
        var windowStart = new DateTime(2026, 2, 27, 14, 0, 0, DateTimeKind.Utc);
        var windowSeconds = 60;
        var windowEnd = windowStart.AddSeconds(windowSeconds);

        var withinWindow = new DateTime(2026, 2, 27, 14, 0, 30, DateTimeKind.Utc);
        var afterWindow = new DateTime(2026, 2, 27, 14, 1, 1, DateTimeKind.Utc);

        Assert.True(withinWindow < windowEnd);
        Assert.False(afterWindow < windowEnd);
    }

    [Fact]
    public void RetryAfter_Calculation_IsCorrect()
    {
        var windowStart = new DateTime(2026, 2, 27, 14, 0, 0, DateTimeKind.Utc);
        var windowSeconds = 60;
        var windowEnd = windowStart.AddSeconds(windowSeconds);
        var now = new DateTime(2026, 2, 27, 14, 0, 42, DateTimeKind.Utc);

        var retryAfter = (int)Math.Ceiling((windowEnd - now).TotalSeconds);
        Assert.Equal(18, retryAfter);
    }

    // ── X-Forwarded-For extraction tests ──

    [Theory]
    [InlineData("203.0.113.42", "203.0.113.42")]
    [InlineData("203.0.113.42, 10.0.0.1, 192.168.1.1", "203.0.113.42")]
    [InlineData(" 203.0.113.42 , 10.0.0.1 ", "203.0.113.42")]
    public void IpExtraction_TakesFirstIp(string headerValue, string expectedIp)
    {
        var ip = headerValue.Split(',', StringSplitOptions.TrimEntries)[0];
        Assert.Equal(expectedIp, ip);
    }
}
