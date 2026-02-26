namespace Candour.Infrastructure.Tests;

using Candour.Infrastructure.Crypto;

public class BlindTokenServiceTests
{
    private static BlindTokenService CreateService() => new();

    [Fact]
    public void GenerateBatchSecret_ReturnsBase64Of32Bytes()
    {
        var service = CreateService();

        var secret = service.GenerateBatchSecret();

        var decoded = Convert.FromBase64String(secret);
        Assert.Equal(32, decoded.Length);
    }

    [Fact]
    public void GenerateBatchSecret_ProducesUniqueValues()
    {
        var service = CreateService();

        var secrets = Enumerable.Range(0, 10).Select(_ => service.GenerateBatchSecret()).ToList();

        Assert.Equal(10, secrets.Distinct().Count());
    }

    [Fact]
    public void GenerateToken_ReturnsCompoundFormat()
    {
        var service = CreateService();
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
        var service = CreateService();
        var secret = service.GenerateBatchSecret();

        var tokens = Enumerable.Range(0, 20).Select(_ => service.GenerateToken(secret)).ToList();

        Assert.Equal(20, tokens.Distinct().Count());
    }

    [Fact]
    public void HashToken_Returns64CharLowercaseHex()
    {
        var service = CreateService();
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var hash = service.HashToken(token);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashToken_IsDeterministic()
    {
        var service = CreateService();

        var hash1 = service.HashToken("test-token-value");
        var hash2 = service.HashToken("test-token-value");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        var service = CreateService();

        var hash1 = service.HashToken("token-a");
        var hash2 = service.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ValidateToken_AcceptsGeneratedToken()
    {
        var service = CreateService();
        var secret = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret);

        var isValid = service.ValidateToken(token, secret);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsNonBase64()
    {
        var service = CreateService();
        var secret = service.GenerateBatchSecret();

        var isValid = service.ValidateToken("not!!!base64", secret);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsTamperedHmac()
    {
        var service = CreateService();
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
        var service = CreateService();
        var secret1 = service.GenerateBatchSecret();
        var secret2 = service.GenerateBatchSecret();
        var token = service.GenerateToken(secret1);

        var isValid = service.ValidateToken(token, secret2);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_RejectsTamperedNonce()
    {
        var service = CreateService();
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
        var service = CreateService();
        var secret = service.GenerateBatchSecret();

        var isValid = service.ValidateToken("", secret);

        Assert.False(isValid);
    }
}
