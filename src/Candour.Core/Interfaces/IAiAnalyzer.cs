namespace Candour.Core.Interfaces;

using Candour.Core.ValueObjects;

public interface IAiAnalyzer
{
    Task<AnalysisReport> AnalyzeAsync(AggregateData data, CancellationToken ct = default);
}
