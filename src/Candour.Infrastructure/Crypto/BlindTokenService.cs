namespace Candour.Infrastructure.Crypto;

using System.Security.Cryptography;
using System.Text;
using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class BlindTokenService : ITokenService
{
    private readonly CandourDbContext _db;

    public BlindTokenService(CandourDbContext db) => _db = db;

    public string GenerateBatchSecret()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    public string GenerateToken(string batchSecret)
    {
        var nonce = new byte[16]; // 128-bit CSPRNG nonce
        RandomNumberGenerator.Fill(nonce);

        var keyBytes = Convert.FromBase64String(batchSecret);
        using var hmac = new HMACSHA256(keyBytes);
        var mac = hmac.ComputeHash(nonce);

        return Convert.ToBase64String(nonce) + "." + Convert.ToBase64String(mac);
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public bool ValidateToken(string token, string batchSecret)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            var nonce = Convert.FromBase64String(parts[0]);
            var providedMac = Convert.FromBase64String(parts[1]);

            if (nonce.Length != 16)
                return false;

            var keyBytes = Convert.FromBase64String(batchSecret);
            using var hmac = new HMACSHA256(keyBytes);
            var computedMac = hmac.ComputeHash(nonce);

            return CryptographicOperations.FixedTimeEquals(computedMac, providedMac);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsTokenUsedAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
        => await _db.UsedTokens.AnyAsync(t => t.TokenHash == tokenHash && t.SurveyId == surveyId, ct);

    public async Task MarkTokenUsedAsync(string tokenHash, Guid surveyId, CancellationToken ct = default)
    {
        _db.UsedTokens.Add(new UsedToken
        {
            TokenHash = tokenHash,
            SurveyId = surveyId
        });
        await _db.SaveChangesAsync(ct);
    }
}
