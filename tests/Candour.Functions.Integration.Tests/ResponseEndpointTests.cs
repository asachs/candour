namespace Candour.Functions.Integration.Tests;

using Candour.Application.Responses;
using Candour.Core.Entities;
using Candour.Core.Enums;
using Moq;

/// <summary>
/// Integration tests for response submission and token validation
/// dispatched through real MediatR pipeline with mocked crypto services.
/// </summary>
public class ResponseEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ResponseEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SubmitResponse_ValidToken_Succeeds()
    {
        var surveyId = Guid.NewGuid();
        var token = "valid-token";
        var secret = "batch-secret";

        _fixture.SurveyRepo
            .Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Survey { Id = surveyId, Status = SurveyStatus.Active, BatchSecret = "encrypted" });

        _fixture.Protector
            .Setup(p => p.Unprotect("encrypted"))
            .Returns(secret);

        _fixture.TokenService
            .Setup(t => t.ValidateToken(token, secret))
            .Returns(true);

        _fixture.TokenService
            .Setup(t => t.HashToken(token))
            .Returns("token-hash");

        _fixture.UsedTokenRepo
            .Setup(r => r.ExistsAsync("token-hash", surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _fixture.JitterService
            .Setup(j => j.ApplyJitter(It.IsAny<DateTime>(), It.IsAny<int>()))
            .Returns<DateTime, int>((dt, _) => dt);

        _fixture.ResponseRepo
            .Setup(r => r.AddAsync(It.IsAny<SurveyResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SurveyResponse());

        var result = await _fixture.Mediator.Send(
            new SubmitResponseCommand(surveyId, token, new Dictionary<string, string> { ["q1"] = "Yes" }));

        Assert.True(result.Success);
        _fixture.UsedTokenRepo.Verify(r => r.AddAsync("token-hash", surveyId, It.IsAny<CancellationToken>()), Times.Once);
        _fixture.ResponseRepo.Verify(r => r.AddAsync(It.IsAny<SurveyResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitResponse_UsedToken_ReturnsError()
    {
        var surveyId = Guid.NewGuid();
        var token = "used-token";
        var secret = "batch-secret";

        _fixture.SurveyRepo
            .Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Survey { Id = surveyId, Status = SurveyStatus.Active, BatchSecret = "encrypted" });

        _fixture.Protector
            .Setup(p => p.Unprotect("encrypted"))
            .Returns(secret);

        _fixture.TokenService
            .Setup(t => t.ValidateToken(token, secret))
            .Returns(true);

        _fixture.TokenService
            .Setup(t => t.HashToken(token))
            .Returns("used-hash");

        _fixture.UsedTokenRepo
            .Setup(r => r.ExistsAsync("used-hash", surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _fixture.Mediator.Send(
            new SubmitResponseCommand(surveyId, token, new Dictionary<string, string> { ["q1"] = "Yes" }));

        Assert.False(result.Success);
        Assert.Equal("Token already used", result.Error);
    }

    [Fact]
    public async Task ValidateToken_ValidUnusedToken_ReturnsValid()
    {
        var surveyId = Guid.NewGuid();
        var token = "fresh-token";
        var secret = "batch-secret";

        _fixture.SurveyRepo
            .Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Survey { Id = surveyId, Status = SurveyStatus.Active, BatchSecret = "encrypted" });

        _fixture.Protector
            .Setup(p => p.Unprotect("encrypted"))
            .Returns(secret);

        _fixture.TokenService
            .Setup(t => t.ValidateToken(token, secret))
            .Returns(true);

        _fixture.TokenService
            .Setup(t => t.HashToken(token))
            .Returns("fresh-hash");

        _fixture.UsedTokenRepo
            .Setup(r => r.ExistsAsync("fresh-hash", surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _fixture.Mediator.Send(new ValidateTokenQuery(surveyId, token));

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateToken_MissingSurvey_ReturnsError()
    {
        var surveyId = Guid.NewGuid();

        _fixture.SurveyRepo
            .Setup(r => r.GetByIdAsync(surveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var result = await _fixture.Mediator.Send(new ValidateTokenQuery(surveyId, "any-token"));

        Assert.False(result.Valid);
        Assert.Equal("Survey not found", result.Error);
    }
}
