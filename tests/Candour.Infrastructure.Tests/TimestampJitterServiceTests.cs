namespace Candour.Infrastructure.Tests;

using Candour.Infrastructure.Crypto;

public class TimestampJitterServiceTests
{
    [Fact]
    public void ApplyJitter_ZeroMinutes_ReturnsSameTimestamp()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = service.ApplyJitter(original, 0);

        Assert.Equal(original, result);
    }

    [Fact]
    public void ApplyJitter_NegativeMinutes_ReturnsSameTimestamp()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var result = service.ApplyJitter(original, -5);

        Assert.Equal(original, result);
    }

    [Fact]
    public void ApplyJitter_PositiveMinutes_ResultWithinExpectedRange()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var jitterMinutes = 10;

        var min = original.AddMinutes(-jitterMinutes);
        var max = original.AddMinutes(jitterMinutes);

        for (int i = 0; i < 200; i++)
        {
            var result = service.ApplyJitter(original, jitterMinutes);
            Assert.InRange(result, min, max);
        }
    }

    [Fact]
    public void ApplyJitter_ProducesVariation()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var jitterMinutes = 30;

        var results = Enumerable.Range(0, 200)
            .Select(_ => service.ApplyJitter(original, jitterMinutes))
            .ToHashSet();

        // With 200 iterations and a 61-minute range, we should see multiple distinct values
        Assert.True(results.Count > 1, $"Expected variation but got {results.Count} distinct value(s)");
    }

    [Fact]
    public void ApplyJitter_SmallJitter_StillAppliesOffset()
    {
        var service = new TimestampJitterService();
        var original = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // With jitterMinutes=1, range is [-1, +1] so 3 possible outcomes: -1, 0, +1
        var sawDifferent = false;
        for (int i = 0; i < 100; i++)
        {
            var result = service.ApplyJitter(original, 1);
            if (result != original)
            {
                sawDifferent = true;
                break;
            }
        }

        Assert.True(sawDifferent, "Expected at least one jittered result to differ from original in 100 iterations");
    }
}
