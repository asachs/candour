namespace Candour.Core.Interfaces;

using Candour.Core.Entities;

public interface IRateLimitRepository
{
    Task<RateLimitEntry?> GetAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(RateLimitEntry entry, CancellationToken ct = default);
}
