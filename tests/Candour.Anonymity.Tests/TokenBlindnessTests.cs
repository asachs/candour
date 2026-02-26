namespace Candour.Anonymity.Tests;

using Candour.Infrastructure.Crypto;

public class TokenBlindnessTests
{
    [Fact]
    public void GenerateBatchSecret_Returns256BitKey()
    {
        var service = new BlindTokenService();

        var secret = service.GenerateBatchSecret();
        var bytes = Convert.FromBase64String(secret);

        Assert.Equal(32, bytes.Length); // 256 bits = 32 bytes
    }

    [Fact]
    public void GenerateToken_ProducesCompoundNonceHmacFormat()
    {
        var service = new BlindTokenService();
        var secret = service.GenerateBatchSecret();

        var token = service.GenerateToken(secret);
        var parts = token.Split('.');

        Assert.Equal(2, parts.Length);

        var nonce = Convert.FromBase64String(parts[0]);
        var hmac = Convert.FromBase64String(parts[1]);

        Assert.Equal(16, nonce.Length); // 128-bit CSPRNG nonce
        Assert.Equal(32, hmac.Length); // HMAC-SHA256 output is 32 bytes
    }

    [Fact]
    public void HashToken_ProducesConsistentSha256Hash()
    {
        var service = new BlindTokenService();
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
        var service = new BlindTokenService();
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var hash = service.HashToken(token);

        // Hash is one-way -- cannot contain or derive the original token
        Assert.DoesNotContain(token, hash);
        Assert.NotEqual(token, hash);
    }

    [Fact]
    public void DifferentTokens_ProduceDifferentHashes()
    {
        var service = new BlindTokenService();
        var secret = service.GenerateBatchSecret();

        var token1 = service.GenerateToken(secret);
        var token2 = service.GenerateToken(secret);

        Assert.NotEqual(token1, token2); // Different nonces
        Assert.NotEqual(service.HashToken(token1), service.HashToken(token2));
    }
}
