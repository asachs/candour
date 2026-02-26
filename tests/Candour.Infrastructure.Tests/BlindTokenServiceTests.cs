namespace Candour.Infrastructure.Tests;

using Candour.Infrastructure.Crypto;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class BlindTokenServiceTests
{
    private static CandourDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CandourDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CandourDbContext(options);
    }

    [Fact]
    public void GenerateBatchSecret_ReturnsBase64Of32Bytes()
    {
        using var ctx = CreateContext(nameof(GenerateBatchSecret_ReturnsBase64Of32Bytes));
        var service = new BlindTokenService(ctx);

        var secret = service.GenerateBatchSecret();

        var decoded = Convert.FromBase64String(secret);
        Assert.Equal(32, decoded.Length);
    }

    [Fact]
    public void GenerateBatchSecret_ProducesUniqueValues()
    {
        using var ctx = CreateContext(nameof(GenerateBatchSecret_ProducesUniqueValues));
        var service = new BlindTokenService(ctx);

        var secrets = Enumerable.Range(0, 10).Select(_ => service.GenerateBatchSecret()).ToList();

        Assert.Equal(10, secrets.Distinct().Count());
    }

    [Fact]
    public void GenerateToken_ReturnsCompoundFormat()
    {
        using var ctx = CreateContext(nameof(GenerateToken_ReturnsCompoundFormat));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();

        var token = service.GenerateToken(secret);

        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);

        var nonce = Convert.FromBase64String(parts[0]);
        var hmac = Convert.FromBase64String(parts[1]);
        Assert.Equal(16, nonce.Length); // 128-bit CSPRNG nonce
        Assert.Equal(32, hmac.Length); // HMAC-SHA256 output
    }

    [Fact]
    public void GenerateToken_DifferentCallsProduceDifferentTokens()
    {
        using var ctx = CreateContext(nameof(GenerateToken_DifferentCallsProduceDifferentTokens));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();

        var tokens = Enumerable.Range(0, 20).Select(_ => service.GenerateToken(secret)).ToList();

        Assert.Equal(20, tokens.Distinct().Count());
    }

    [Fact]
    public void HashToken_Returns64CharLowercaseHex()
    {
        using var ctx = CreateContext(nameof(HashToken_Returns64CharLowercaseHex));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var hash = service.HashToken(token);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashToken_IsDeterministic()
    {
        using var ctx = CreateContext(nameof(HashToken_IsDeterministic));
        var service = new BlindTokenService(ctx);

        var hash1 = service.HashToken("test-token-value");
        var hash2 = service.HashToken("test-token-value");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        using var ctx = CreateContext(nameof(HashToken_DifferentInputsProduceDifferentHashes));
        var service = new BlindTokenService(ctx);

        var hash1 = service.HashToken("token-a");
        var hash2 = service.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ValidateToken_AcceptsGeneratedToken()
    {
        using var ctx = CreateContext(nameof(ValidateToken_AcceptsGeneratedToken));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var isValid = service.ValidateToken(token, secret);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsNonBase64()
    {
        using var ctx = CreateContext(nameof(ValidateToken_RejectsNonBase64));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();

        var isValid = service.ValidateToken("not!!!base64", secret);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsTamperedHmac()
    {
        using var ctx = CreateContext(nameof(ValidateToken_RejectsTamperedHmac));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        // Tamper with the HMAC part
        var parts = token.Split('.');
        var hmacBytes = Convert.FromBase64String(parts[1]);
        hmacBytes[0] ^= 0xFF; // flip bits in first byte
        var tampered = parts[0] + "." + Convert.ToBase64String(hmacBytes);

        var isValid = service.ValidateToken(tampered, secret);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsTokenWithWrongSecret()
    {
        using var ctx = CreateContext(nameof(ValidateToken_RejectsTokenWithWrongSecret));
        var service = new BlindTokenService(ctx);
        var secret1 = service.GenerateBatchSecret();
        var secret2 = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret1);

        var isValid = service.ValidateToken(token, secret2);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsTamperedNonce()
    {
        using var ctx = CreateContext(nameof(ValidateToken_RejectsTamperedNonce));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        // Tamper with the nonce part
        var parts = token.Split('.');
        var nonceBytes = Convert.FromBase64String(parts[0]);
        nonceBytes[0] ^= 0xFF; // flip bits in first byte
        var tampered = Convert.ToBase64String(nonceBytes) + "." + parts[1];

        var isValid = service.ValidateToken(tampered, secret);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsEmptyString()
    {
        using var ctx = CreateContext(nameof(ValidateToken_RejectsEmptyString));
        var service = new BlindTokenService(ctx);
        var secret = service.GenerateBatchSecret();

        var isValid = service.ValidateToken("", secret);

        Assert.False(isValid);
    }

    [Fact]
    public async Task IsTokenUsedAsync_ReturnsFalseForUnusedToken()
    {
        using var ctx = CreateContext(nameof(IsTokenUsedAsync_ReturnsFalseForUnusedToken));
        var service = new BlindTokenService(ctx);

        var isUsed = await service.IsTokenUsedAsync("somehash", Guid.NewGuid());

        Assert.False(isUsed);
    }

    [Fact]
    public async Task MarkTokenUsedAsync_ThenIsTokenUsedAsync_ReturnsTrue()
    {
        using var ctx = CreateContext(nameof(MarkTokenUsedAsync_ThenIsTokenUsedAsync_ReturnsTrue));
        var service = new BlindTokenService(ctx);
        var surveyId = Guid.NewGuid();
        var tokenHash = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890";

        await service.MarkTokenUsedAsync(tokenHash, surveyId);
        var isUsed = await service.IsTokenUsedAsync(tokenHash, surveyId);

        Assert.True(isUsed);
    }

    [Fact]
    public async Task IsTokenUsedAsync_IsScopedToSurvey()
    {
        using var ctx = CreateContext(nameof(IsTokenUsedAsync_IsScopedToSurvey));
        var service = new BlindTokenService(ctx);
        var surveyA = Guid.NewGuid();
        var surveyB = Guid.NewGuid();
        var tokenHash = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890";

        await service.MarkTokenUsedAsync(tokenHash, surveyA);

        Assert.True(await service.IsTokenUsedAsync(tokenHash, surveyA));
        Assert.False(await service.IsTokenUsedAsync(tokenHash, surveyB));
    }
}
