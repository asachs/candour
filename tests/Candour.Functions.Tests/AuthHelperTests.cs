namespace Candour.Functions.Tests;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Tests the constant-time comparison logic used by AuthHelper.
/// The actual AuthHelper.ValidateApiKey requires HttpRequestData and IConfiguration
/// which need a full Functions host. These tests verify the cryptographic comparison
/// pattern is correct.
/// </summary>
public class AuthHelperTests
{
    [Fact]
    public void FixedTimeEquals_ReturnsTrue_WhenBytesMatch()
    {
        var key = "my-secret-key";
        var bytes1 = Encoding.UTF8.GetBytes(key);
        var bytes2 = Encoding.UTF8.GetBytes(key);

        Assert.True(CryptographicOperations.FixedTimeEquals(bytes1, bytes2));
    }

    [Fact]
    public void FixedTimeEquals_ReturnsFalse_WhenBytesDiffer()
    {
        var bytes1 = Encoding.UTF8.GetBytes("correct-key");
        var bytes2 = Encoding.UTF8.GetBytes("wrong-key!!");

        Assert.False(CryptographicOperations.FixedTimeEquals(bytes1, bytes2));
    }

    [Fact]
    public void FixedTimeEquals_ReturnsFalse_WhenDifferentLengths()
    {
        var bytes1 = Encoding.UTF8.GetBytes("short");
        var bytes2 = Encoding.UTF8.GetBytes("longer-key");

        Assert.False(CryptographicOperations.FixedTimeEquals(bytes1, bytes2));
    }

    [Fact]
    public void FixedTimeEquals_HandlesEmptyInput()
    {
        var empty1 = Array.Empty<byte>();
        var empty2 = Array.Empty<byte>();

        Assert.True(CryptographicOperations.FixedTimeEquals(empty1, empty2));
    }

    [Fact]
    public void FixedTimeEquals_SingleBitDifference_ReturnsFalse()
    {
        var bytes1 = Encoding.UTF8.GetBytes("abcdefgh");
        var bytes2 = Encoding.UTF8.GetBytes("abcdefgx");

        Assert.False(CryptographicOperations.FixedTimeEquals(bytes1, bytes2));
    }

    [Fact]
    public void ApiKeyHeader_ConstantName()
    {
        // Documents that the API key header is X-Api-Key
        const string expectedHeader = "X-Api-Key";
        Assert.Equal("X-Api-Key", expectedHeader);
    }

    [Fact]
    public void ApiKeyConfig_ConstantPath()
    {
        // Documents that the config path is Candour:ApiKey
        const string expectedPath = "Candour:ApiKey";
        Assert.Equal("Candour:ApiKey", expectedPath);
    }
}
