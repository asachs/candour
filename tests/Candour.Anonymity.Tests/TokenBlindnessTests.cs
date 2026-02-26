namespace Candour.Anonymity.Tests;

using Candour.Infrastructure.Crypto;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class TokenBlindnessTests
{
    private CandourDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<CandourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CandourDbContext(options);
    }

    [Fact]
    public void GenerateBatchSecret_Returns256BitKey()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);

        var secret = service.GenerateBatchSecret();
        var bytes = Convert.FromBase64String(secret);

        Assert.Equal(32, bytes.Length); // 256 bits = 32 bytes
    }

    [Fact]
    public void GenerateToken_ProducesValidHmacSha256Output()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);
        var secret = service.GenerateBatchSecret();

        var token = service.GenerateToken(secret);
        var decoded = Convert.FromBase64String(token);

        Assert.Equal(32, decoded.Length); // HMAC-SHA256 output is 32 bytes
    }

    [Fact]
    public void HashToken_ProducesConsistentSha256Hash()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var hash1 = service.HashToken(token);
        var hash2 = service.HashToken(token);

        Assert.Equal(hash1, hash2); // Same input = same hash
        Assert.Equal(64, hash1.Length); // SHA256 hex is 64 chars
    }

    [Fact]
    public void HashToken_CannotBeReversedToOriginalToken()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var hash = service.HashToken(token);

        // Hash is one-way — cannot contain or derive the original token
        Assert.DoesNotContain(token, hash);
        Assert.NotEqual(token, hash);
    }

    [Fact]
    public async Task DuplicateToken_IsRejected()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);
        var hash = service.HashToken(token);
        var surveyId = Guid.NewGuid();

        Assert.False(await service.IsTokenUsedAsync(hash, surveyId));

        await service.MarkTokenUsedAsync(hash, surveyId);

        Assert.True(await service.IsTokenUsedAsync(hash, surveyId));
    }

    [Fact]
    public void DifferentTokens_ProduceDifferentHashes()
    {
        using var db = CreateInMemoryDb();
        var service = new BlindTokenService(db);
        var secret = service.GenerateBatchSecret();

        var token1 = service.GenerateToken(secret);
        var token2 = service.GenerateToken(secret);

        Assert.NotEqual(token1, token2); // Different nonces
        Assert.NotEqual(service.HashToken(token1), service.HashToken(token2));
    }
}
