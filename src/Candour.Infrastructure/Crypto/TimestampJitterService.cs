namespace Candour.Infrastructure.Crypto;

using System.Security.Cryptography;
using Candour.Core.Interfaces;

public class TimestampJitterService : ITimestampJitterService
{
    public DateTime ApplyJitter(DateTime timestamp, int jitterMinutes)
    {
        if (jitterMinutes <= 0) return timestamp;

        var range = jitterMinutes * 2; // +/-jitterMinutes
        var offsetMinutes = RandomNumberGenerator.GetInt32(range + 1) - jitterMinutes;
        return timestamp.AddMinutes(offsetMinutes);
    }
}
