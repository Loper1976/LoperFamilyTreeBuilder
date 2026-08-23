namespace ResearchAgent.Core.AI;

public sealed record ProviderHealth(string Provider, string Model, bool Available, TimeSpan? Latency, string? Error, DateTimeOffset CheckedUtc);
public sealed record BenchmarkResult(string Provider, string Model, string TaskType, decimal Accuracy, decimal HallucinationPenalty, decimal CostScore, decimal SpeedScore, DateTimeOffset TestedUtc);

public sealed class ProviderRegistry
{
    private readonly Dictionary<(string Provider, string Model), ModelCapability> _models = new();
    private readonly List<BenchmarkResult> _benchmarks = [];
    private readonly Dictionary<(string Provider, string Model), ProviderHealth> _health = new();

    public void Register(ModelCapability model) => _models[(model.Provider, model.Model)] = model;
    public void RecordBenchmark(BenchmarkResult result) => _benchmarks.Add(result);
    public void RecordHealth(ProviderHealth health) => _health[(health.Provider, health.Model)] = health;

    public IReadOnlyList<ModelCapability> AvailableModels()
    {
        return _models.Values.Select(m =>
        {
            var healthy = !_health.TryGetValue((m.Provider, m.Model), out var h) || h.Available;
            return m with { IsAvailable = m.IsAvailable && healthy };
        }).Where(m => m.IsAvailable).ToArray();
    }

    public decimal? EffectiveBenchmark(string provider, string model, string taskType)
    {
        var rows = _benchmarks.Where(b => b.Provider == provider && b.Model == model &&
            b.TaskType.Equals(taskType, StringComparison.OrdinalIgnoreCase)).ToList();
        if (rows.Count == 0) return null;
        return rows.Average(b => Math.Clamp(b.Accuracy - b.HallucinationPenalty, 0m, 100m));
    }
}
