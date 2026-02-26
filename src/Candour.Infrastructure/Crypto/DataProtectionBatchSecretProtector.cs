namespace Candour.Infrastructure.Crypto;

using Candour.Core.Interfaces;
using Microsoft.AspNetCore.DataProtection;

public class DataProtectionBatchSecretProtector : IBatchSecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionBatchSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Candour.BatchSecret");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
