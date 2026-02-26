namespace Candour.Anonymity.Tests;

using Candour.Core.Entities;
using System.Reflection;

public class ResponseUnlinkabilityTests
{
    [Fact]
    public void SurveyResponse_HasNoIdentityFields()
    {
        var type = typeof(SurveyResponse);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var forbiddenNames = new[]
        {
            "RespondentId", "UserId", "IpAddress", "Ip",
            "UserAgent", "Email", "TokenReference", "Token",
            "SessionId", "CookieId", "DeviceId", "Fingerprint"
        };

        foreach (var prop in properties)
        {
            Assert.DoesNotContain(prop.Name, forbiddenNames);
        }
    }

    [Fact]
    public void SurveyResponse_HasExactlyFourProperties()
    {
        var type = typeof(SurveyResponse);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Only: Id, SurveyId, Answers, SubmittedAt
        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void SurveyResponse_PropertiesAreCorrect()
    {
        var type = typeof(SurveyResponse);
        var propNames = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(new[] { "Answers", "Id", "SubmittedAt", "SurveyId" }, propNames);
    }

    [Fact]
    public void SurveyResponse_IdIsRandomGuid()
    {
        var r1 = new SurveyResponse();
        var r2 = new SurveyResponse();

        Assert.NotEqual(r1.Id, r2.Id);
        Assert.NotEqual(Guid.Empty, r1.Id);
    }

    [Fact]
    public void UsedToken_HasNoNavigationToResponse()
    {
        var type = typeof(UsedToken);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Should NOT have any Response/SurveyResponse navigation property
        Assert.DoesNotContain(properties, p =>
            p.PropertyType == typeof(SurveyResponse) ||
            p.Name.Contains("Response"));
    }
}
