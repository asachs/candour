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
        var nonce = Guid.NewGuid().ToString();
        var keyBytes = Convert.FromBase64String(batchSecret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(nonce));
        return Convert.ToBase64String(hash);
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public bool ValidateToken(string token, string batchSecret)
    {
        // Token is a valid HMAC-SHA256 output (base64, 32 bytes decoded)
        try
        {
            var decoded = Convert.FromBase64String(token);
            return decoded.Length == 32; // SHA256 output is 32 bytes
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
            SurveyId = surveyId,
            UsedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
