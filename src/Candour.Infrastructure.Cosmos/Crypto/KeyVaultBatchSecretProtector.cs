namespace Candour.Infrastructure.Cosmos.Crypto;

using Azure.Security.KeyVault.Keys.Cryptography;
using Candour.Core.Interfaces;

public class KeyVaultBatchSecretProtector : IBatchSecretProtector
{
    private readonly CryptographyClient _cryptoClient;

    public KeyVaultBatchSecretProtector(CryptographyClient cryptoClient)
    {
        _cryptoClient = cryptoClient;
    }

    public string Protect(string plainText)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var result = _cryptoClient.WrapKey(KeyWrapAlgorithm.RsaOaep256, plainBytes);
        return Convert.ToBase64String(result.EncryptedKey);
    }

    public string Unprotect(string protectedText)
    {
        var encryptedBytes = Convert.FromBase64String(protectedText);
        var result = _cryptoClient.UnwrapKey(KeyWrapAlgorithm.RsaOaep256, encryptedBytes);
        return System.Text.Encoding.UTF8.GetString(result.Key);
    }
}
