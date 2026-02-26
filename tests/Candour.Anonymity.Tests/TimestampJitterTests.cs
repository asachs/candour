namespace Candour.Anonymity.Tests;

using Candour.Infrastructure.Crypto;

public class TimestampJitterTests
{
    [Fact]
    public void ApplyJitter_ModifiesTimestamp()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // With enough runs, at least one should differ
        var results = Enumerable.Range(0, 100)
            .Select(_ => service.ApplyJitter(original, 10))
            .ToList();

        Assert.Contains(results, t => t != original);
    }

    [Fact]
    public void ApplyJitter_StaysWithinRange()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var jitterMinutes = 10;

        for (int i = 0; i < 1000; i++)
        {
            var jittered = service.ApplyJitter(original, jitterMinutes);
            var diff = Math.Abs((jittered - original).TotalMinutes);
            Assert.True(diff <= jitterMinutes, $"Jitter {diff} exceeded {jitterMinutes} minutes");
        }
    }

    [Fact]
    public void ApplyJitter_ZeroJitter_ReturnsOriginal()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = service.ApplyJitter(original, 0);

        Assert.Equal(original, result);
    }
}
