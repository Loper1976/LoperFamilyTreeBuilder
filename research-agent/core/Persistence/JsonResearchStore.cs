using System.Text.Json;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Proof;
using ResearchAgent.Core.Research;
using ResearchAgent.Core.UI;

namespace ResearchAgent.Core.Persistence;

public sealed class JsonResearchStore : IResearchCenterQueryStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonResearchStore(string root)
    {
        _root = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));
        Directory.CreateDirectory(_root);
    }

    public Task SaveSourceAsync(ResearchSource x, CancellationToken ct = default) => SaveAsync("sources", x.Id, x, ct);
    public Task SaveCitationAsync(Citation x, CancellationToken ct = default) => SaveAsync("citations", x.Id, x, ct);
    public Task SaveClaimAsync(ResearchClaim x, CancellationToken ct = default) => SaveAsync("claims", x.Id, x, ct);
    public Task SaveEvidenceAsync(EvidenceLink x, CancellationToken ct = default) => SaveAsync("evidence", x.Id, x, ct);
    public Task SaveAiAnalysisAsync(AiAnalysis x, CancellationToken ct = default) => SaveAsync("ai-analyses", x.Id, x, ct);
    public Task SaveResearchTaskAsync(ResearchTask x, CancellationToken ct = default) => SaveAsync("research-tasks", x.Id, x, ct);
    public Task SaveProofPacketAsync(ProofPacket x, CancellationToken ct = default) => SaveAsync("proof-packets", x.Id, x, ct);

    public Task<ResearchSource?> GetSourceAsync(Guid id, CancellationToken ct = default) => LoadAsync<ResearchSource>("sources", id, ct);
    public Task<ResearchClaim?> GetClaimAsync(Guid id, CancellationToken ct = default) => LoadAsync<ResearchClaim>("claims", id, ct);

    public async Task<IReadOnlyList<EvidenceLink>> GetEvidenceForClaimAsync(Guid claimId, CancellationToken ct = default) =>
        (await LoadAllAsync<EvidenceLink>("evidence", ct)).Where(x => x.ClaimId == claimId).ToArray();

    public async Task<IReadOnlyList<ResearchTask>> GetOpenTasksAsync(Guid? personId = null, CancellationToken ct = default) =>
        (await LoadAllAsync<ResearchTask>("research-tasks", ct))
            .Where(x => x.Status == ResearchTaskStatus.Open && (personId is null || x.SubjectPersonId == personId)).ToArray();

    public async Task<IReadOnlyList<ResearchClaim>> GetClaimsForPersonAsync(Guid personId, CancellationToken ct = default) =>
        (await LoadAllAsync<ResearchClaim>("claims", ct)).Where(x => x.SubjectPersonId == personId).ToArray();

    public async Task<int> GetSourceCountForPersonAsync(Guid personId, CancellationToken ct = default)
    {
        var claimIds = (await GetClaimsForPersonAsync(personId, ct)).Select(x => x.Id).ToHashSet();
        var citationIds = (await LoadAllAsync<EvidenceLink>("evidence", ct))
            .Where(x => claimIds.Contains(x.ClaimId)).Select(x => x.CitationId).ToHashSet();
        return (await LoadAllAsync<Citation>("citations", ct))
            .Where(x => citationIds.Contains(x.Id)).Select(x => x.SourceId).Distinct().Count();
    }

    public async Task<int> GetProofPacketCountForPersonAsync(Guid personId, CancellationToken ct = default) =>
        (await LoadAllAsync<ProofPacket>("proof-packets", ct)).Count(x => x.SubjectPersonId == personId);

    private async Task SaveAsync<T>(string area, Guid id, T value, CancellationToken ct)
    {
        var dir = Path.Combine(_root, area);
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, $"{id:N}.json");
        var temp = target + ".tmp";
        await _gate.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(value, _json);
            await File.WriteAllTextAsync(temp, json, ct);
            File.Move(temp, target, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
            _gate.Release();
        }
    }

    private async Task<T?> LoadAsync<T>(string area, Guid id, CancellationToken ct)
    {
        var path = Path.Combine(_root, area, $"{id:N}.json");
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
    }

    private async Task<IReadOnlyList<T>> LoadAllAsync<T>(string area, CancellationToken ct)
    {
        var dir = Path.Combine(_root, area);
        if (!Directory.Exists(dir)) return [];
        var result = new List<T>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            ct.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var item = await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
            if (item is not null) result.Add(item);
        }
        return result;
    }
}
