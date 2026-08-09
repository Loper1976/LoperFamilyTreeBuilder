using System.Text.Json;
using System.Text.Json.Serialization;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;

namespace LoperFamilyTreeBuilder.Data.Services;

/// <summary>
/// Durable local store for research work products that do not alter authoritative genealogy.
/// These records are research overlays only. They never renumber, normalize, or rewrite Legacy Numbers.
/// </summary>
public sealed class ResearchIntelligenceStore(ApplicationPaths applicationPaths)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _path = Path.Combine(applicationPaths.ConfigurationDirectory, "research-intelligence.json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ResearchIntelligenceState> LoadAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadUnlockedAsync(cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(ResearchIntelligenceState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
            }
            File.Move(temp, _path, true);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<SavedArchiveSearch> SaveSearchAsync(
        string name,
        ArchiveSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A saved search name is required.", nameof(name));
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadUnlockedAsync(cancellationToken);
            var existing = state.SavedSearches.FirstOrDefault(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new SavedArchiveSearch { Name = name.Trim(), Request = request };
                state.SavedSearches.Add(existing);
            }
            else
            {
                existing.Request = request;
                existing.UpdatedUtc = DateTimeOffset.UtcNow;
            }
            await SaveUnlockedAsync(state, cancellationToken);
            return existing;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task DeleteSavedSearchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadUnlockedAsync(cancellationToken);
            state.SavedSearches.RemoveAll(x => x.Id == id);
            await SaveUnlockedAsync(state, cancellationToken);
        }
        finally { Gate.Release(); }
    }

    public async Task AddAliasAsync(PersonNameAliasRecord alias, CancellationToken cancellationToken = default)
    {
        if (alias.PersonId == Guid.Empty || string.IsNullOrWhiteSpace(alias.Alias))
            throw new ArgumentException("Person and alias are required.");
        await MutateAsync(s => s.Aliases.Add(alias), cancellationToken);
    }

    public Task DeleteAliasAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(s => s.Aliases.RemoveAll(x => x.Id == id), cancellationToken);

    public async Task AddProofWorkspaceAsync(ProofWorkspaceRecord workspace, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspace.ResearchQuestion))
            throw new ArgumentException("A research question is required.");
        await MutateAsync(s => s.ProofWorkspaces.Add(workspace), cancellationToken);
    }

    public async Task UpdateProofWorkspaceAsync(ProofWorkspaceRecord workspace, CancellationToken cancellationToken = default)
    {
        await MutateAsync(s =>
        {
            var index = s.ProofWorkspaces.FindIndex(x => x.Id == workspace.Id);
            workspace.UpdatedUtc = DateTimeOffset.UtcNow;
            if (index >= 0) s.ProofWorkspaces[index] = workspace;
            else s.ProofWorkspaces.Add(workspace);
        }, cancellationToken);
    }

    public Task DeleteProofWorkspaceAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(s => s.ProofWorkspaces.RemoveAll(x => x.Id == id), cancellationToken);

    public async Task AddRepositorySearchAsync(RepositorySearchRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.Repository) || string.IsNullOrWhiteSpace(record.SearchQuery))
            throw new ArgumentException("Repository and search query are required.");
        await MutateAsync(s => s.RepositorySearches.Add(record), cancellationToken);
    }

    public Task DeleteRepositorySearchAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(s => s.RepositorySearches.RemoveAll(x => x.Id == id), cancellationToken);

    public async Task AddResearchPlanAsync(ResearchPlanRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.ResearchQuestion))
            throw new ArgumentException("A research question is required.");
        await MutateAsync(s => s.ResearchPlans.Add(record), cancellationToken);
    }

    public async Task UpdateResearchPlanAsync(ResearchPlanRecord record, CancellationToken cancellationToken = default)
    {
        await MutateAsync(s =>
        {
            var index = s.ResearchPlans.FindIndex(x => x.Id == record.Id);
            record.UpdatedUtc = DateTimeOffset.UtcNow;
            if (index >= 0) s.ResearchPlans[index] = record;
            else s.ResearchPlans.Add(record);
        }, cancellationToken);
    }

    public Task DeleteResearchPlanAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(s => s.ResearchPlans.RemoveAll(x => x.Id == id), cancellationToken);

    private async Task MutateAsync(Action<ResearchIntelligenceState> mutation, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadUnlockedAsync(cancellationToken);
            mutation(state);
            await SaveUnlockedAsync(state, cancellationToken);
        }
        finally { Gate.Release(); }
    }

    private async Task<ResearchIntelligenceState> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return new ResearchIntelligenceState();
        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<ResearchIntelligenceState>(stream, JsonOptions, cancellationToken)
                   ?? new ResearchIntelligenceState();
        }
        catch (JsonException)
        {
            // Preserve the unreadable file for recovery instead of overwriting it silently.
            var recovery = _path + $".invalid-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Copy(_path, recovery, false);
            return new ResearchIntelligenceState();
        }
    }

    private async Task SaveUnlockedAsync(ResearchIntelligenceState state, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }
        File.Move(temp, _path, true);
    }
}
