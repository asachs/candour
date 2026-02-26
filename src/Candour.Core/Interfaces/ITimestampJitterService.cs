namespace Candour.Core.Interfaces;

public interface ITimestampJitterService
{
    DateTime ApplyJitter(DateTime timestamp, int jitterMinutes);
}
