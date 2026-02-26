namespace Candour.Core.Interfaces;

public interface IBatchSecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
