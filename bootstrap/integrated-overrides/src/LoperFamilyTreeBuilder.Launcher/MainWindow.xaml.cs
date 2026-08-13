using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Data;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Desktop;

public partial class MainWindow : Window
{
    private readonly List<PersonRow> _allPeople = [];
    private string? _connectionString;
    private bool _peopleLoaded;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _connectionString = FindPackagedConnectionString();
        await LoadPeopleAsync();
    }

    private async void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string section)
        {
            return;
        }

        SectionTitle.Text = section;

        if (section is "People" or "Person Profile")
        {
            DefaultWorkspace.Visibility = Visibility.Collapsed;
            PeopleWorkspace.Visibility = Visibility.Visible;
            if (!_peopleLoaded)
            {
                await LoadPeopleAsync();
            }

            if (section == "Person Profile" && PeopleGrid.SelectedItem is null && PeopleGrid.Items.Count > 0)
            {
                PeopleGrid.SelectedIndex = 0;
            }
            return;
        }

        PeopleWorkspace.Visibility = Visibility.Collapsed;
        DefaultWorkspace.Visibility = Visibility.Visible;
        WorkspaceHeading.Text = section;
        WorkspaceDescription.Text = section switch
        {
            "Family Tree" => "Native desktop family-tree workspace. Legacy Numbers remain read-only historical identifiers and are never recalculated.",
            "Relationships" => "Native relationship management for parent/child and family-union records with integrity safeguards.",
            "Lifetime Timeline" => "Chronological native desktop view for dated and approximate life events, evidence, and locations.",
            "Maps & Migration" => "Native desktop geography workspace for historical places, coordinates, residences, movement, and migration evidence.",
            "Photos" => "Native media archive for photographs, captions, people links, provenance, EXIF, and GPS metadata.",
            "Documents" => "Native document archive connected to people, events, sources, and AI transcription records.",
            "Sources" => "Native source and citation workspace preserving repository details, citations, confidence, and provenance.",
            "Medical & Health" => "Permission-aware native medical and family-health workspace. Medical records remain separate from Legacy Numbers.",
            "DNA Clusters" => "Protected native DNA match workspace. Evidence clustering does not create genealogical relationships automatically.",
            "AI Transcription" => "Native review workspace for document transcription drafts, corrections, confidence, and approval.",
            "Tree Integrity" => "Native review center for genealogy consistency findings. Findings never silently rewrite historical data.",
            "Backup & Restore" => "Desktop backup and restore center for the configured database, media archive, and network-drive copies.",
            "Settings" => "Desktop configuration for archive paths, backup paths, permissions, and application preferences.",
            _ => "Native Windows desktop workspace. Browser hosting is not required for the local application."
        };
    }

    private async void RefreshPeople_Click(object sender, RoutedEventArgs e)
    {
        await LoadPeopleAsync(force: true);
    }

    private void PeopleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyPeopleFilter();
    }

    private async void PeopleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PeopleGrid.SelectedItem is PersonRow selected)
        {
            await LoadProfileAsync(selected.Id);
        }
    }

    private async Task LoadPeopleAsync(bool force = false)
    {
        if (_peopleLoaded && !force)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            PeopleStatus.Text = "Local database configuration was not found. The archive was not modified.";
            return;
        }

        try
        {
            PeopleStatus.Text = "Loading local archive...";
            await using var db = CreateDbContext();

            var people = await db.People
                .AsNoTracking()
                .OrderBy(person => person.Surname)
                .ThenBy(person => person.GivenName)
                .Select(person => new
                {
                    person.Id,
                    person.GivenName,
                    person.MiddleName,
                    person.Surname,
                    person.Suffix,
                    person.BirthDate,
                    person.DeathDate,
                    person.IsLiving
                })
                .ToListAsync();

            var identifiers = await db.PersonIdentifiers
                .AsNoTracking()
                .Where(identifier => identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
                .Select(identifier => new { identifier.PersonId, identifier.Value })
                .ToListAsync();

            var legacyByPerson = identifiers
                .GroupBy(identifier => identifier.PersonId)
                .ToDictionary(group => group.Key, group => group.First().Value);

            _allPeople.Clear();
            _allPeople.AddRange(people.Select(person => new PersonRow(
                person.Id,
                JoinName(person.GivenName, person.MiddleName, person.Surname, person.Suffix),
                legacyByPerson.GetValueOrDefault(person.Id) ?? string.Empty,
                person.BirthDate?.ToString("yyyy-MM-dd") ?? "—",
                person.IsLiving ? "Living" : person.DeathDate?.ToString("yyyy-MM-dd") ?? "—")));

            _peopleLoaded = true;
            ApplyPeopleFilter();
            PeopleStatus.Text = $"{_allPeople.Count:N0} people loaded from the local archive";
        }
        catch (Exception ex)
        {
            PeopleStatus.Text = $"Unable to open local archive: {ex.Message}";
        }
    }

    private void ApplyPeopleFilter()
    {
        var query = PeopleSearchBox?.Text?.Trim() ?? string.Empty;
        IEnumerable<PersonRow> rows = _allPeople;
        if (!string.IsNullOrWhiteSpace(query))
        {
            rows = rows.Where(row =>
                row.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.LegacyNumber.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = rows.Take(2000).ToList();
        PeopleGrid.ItemsSource = filtered;
        if (_peopleLoaded)
        {
            PeopleStatus.Text = string.IsNullOrWhiteSpace(query)
                ? $"{_allPeople.Count:N0} people loaded from the local archive"
                : $"{filtered.Count:N0} matching people";
        }
    }

    private async Task LoadProfileAsync(Guid personId)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        try
        {
            await using var db = CreateDbContext();
            var person = await db.People.AsNoTracking()
                .Where(candidate => candidate.Id == personId)
                .Select(candidate => new
                {
                    candidate.Id,
                    candidate.GivenName,
                    candidate.MiddleName,
                    candidate.Surname,
                    candidate.Suffix,
                    candidate.BirthDate,
                    candidate.DeathDate,
                    candidate.IsLiving
                })
                .SingleOrDefaultAsync();

            if (person is null)
            {
                return;
            }

            var legacy = await db.PersonIdentifiers.AsNoTracking()
                .Where(identifier => identifier.PersonId == personId && identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
                .Select(identifier => identifier.Value)
                .FirstOrDefaultAsync();

            var branches = await db.BranchMemberships.AsNoTracking()
                .Where(membership => membership.PersonId == personId)
                .OrderByDescending(membership => membership.IsPrimary)
                .ThenBy(membership => membership.FamilyBranch.Name)
                .Select(membership => membership.FamilyBranch.Name)
                .ToListAsync();

            var relationships = await db.ParentChildRelationships.AsNoTracking()
                .Where(relationship => relationship.ParentPersonId == personId || relationship.ChildPersonId == personId)
                .Select(relationship => new
                {
                    relationship.ParentPersonId,
                    relationship.ChildPersonId,
                    relationship.RelationshipType
                })
                .ToListAsync();

            var relativeIds = relationships
                .Select(relationship => relationship.ParentPersonId == personId ? relationship.ChildPersonId : relationship.ParentPersonId)
                .Distinct()
                .ToList();

            var relatives = relativeIds.Count == 0
                ? []
                : await db.People.AsNoTracking()
                    .Where(relative => relativeIds.Contains(relative.Id))
                    .Select(relative => new RelativeRow(
                        relative.Id,
                        relative.GivenName,
                        relative.MiddleName,
                        relative.Surname,
                        relative.Suffix))
                    .ToListAsync();

            var relativeLegacy = relativeIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await db.PersonIdentifiers.AsNoTracking()
                    .Where(identifier => relativeIds.Contains(identifier.PersonId) && identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
                    .GroupBy(identifier => identifier.PersonId)
                    .Select(group => new { PersonId = group.Key, Value = group.Select(item => item.Value).First() })
                    .ToDictionaryAsync(item => item.PersonId, item => item.Value);

            var byId = relatives.ToDictionary(relative => relative.Id);
            var parents = relationships
                .Where(relationship => relationship.ChildPersonId == personId && byId.ContainsKey(relationship.ParentPersonId))
                .Select(relationship => FormatRelative(byId[relationship.ParentPersonId], relativeLegacy.GetValueOrDefault(relationship.ParentPersonId), relationship.RelationshipType))
                .OrderBy(value => value)
                .ToList();
            var children = relationships
                .Where(relationship => relationship.ParentPersonId == personId && byId.ContainsKey(relationship.ChildPersonId))
                .Select(relationship => FormatRelative(byId[relationship.ChildPersonId], relativeLegacy.GetValueOrDefault(relationship.ChildPersonId), relationship.RelationshipType))
                .OrderBy(value => value)
                .ToList();

            ProfileName.Text = JoinName(person.GivenName, person.MiddleName, person.Surname, person.Suffix);
            ProfileLegacyNumber.Text = string.IsNullOrWhiteSpace(legacy) ? "Not assigned" : legacy;
            ProfileLife.Text = $"Born: {person.BirthDate?.ToString("MMMM d, yyyy") ?? "Unknown"}\n{(person.IsLiving ? "Status: Living" : $"Died: {person.DeathDate?.ToString("MMMM d, yyyy") ?? "Unknown"}")}";
            ProfileBranches.Text = branches.Count == 0 ? "Not assigned" : string.Join(" • ", branches);
            ProfileParents.ItemsSource = parents.Count == 0 ? ["No parent relationships recorded"] : parents;
            ProfileChildren.ItemsSource = children.Count == 0 ? ["No child relationships recorded"] : children;
            ProfileRecordId.Text = $"Stable Person ID: {person.Id}";
        }
        catch (Exception ex)
        {
            ProfileName.Text = "Profile unavailable";
            ProfileRecordId.Text = ex.Message;
        }
    }

    private FamilyTreeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FamilyTreeDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new FamilyTreeDbContext(options);
    }

    private static string? FindPackagedConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("LOPER_FAMILY_TREE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        var webFolder = Path.Combine(AppContext.BaseDirectory, "Web");
        if (!Directory.Exists(webFolder))
        {
            return null;
        }

        foreach (var file in Directory.EnumerateFiles(webFolder, "appsettings*.json").OrderBy(path => path))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var connection = FindConnectionString(document.RootElement);
                if (!string.IsNullOrWhiteSpace(connection))
                {
                    return connection;
                }
            }
            catch
            {
                // Continue to the next packaged settings file. No data is modified here.
            }
        }

        return null;
    }

    private static string? FindConnectionString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("ConnectionStrings", out var connectionStrings) && connectionStrings.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in connectionStrings.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        return property.Value.GetString();
                    }
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var found = FindConnectionString(property.Value);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return found;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindConnectionString(item);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return found;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (!string.IsNullOrWhiteSpace(value) &&
                (value.Contains("Server=", StringComparison.OrdinalIgnoreCase) || value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)))
            {
                return value;
            }
        }

        return null;
    }

    private static string JoinName(params string?[] parts) =>
        string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    private static string FormatRelative(RelativeRow relative, string? legacy, ParentChildRelationshipType relationshipType)
    {
        var type = relationshipType switch
        {
            ParentChildRelationshipType.Biological => "Biological",
            ParentChildRelationshipType.Adoptive => "Adoptive",
            ParentChildRelationshipType.Step => "Step",
            ParentChildRelationshipType.Foster => "Foster",
            ParentChildRelationshipType.Guardian => "Guardian",
            _ => "Custom"
        };
        var legacyText = string.IsNullOrWhiteSpace(legacy) ? string.Empty : $" • {legacy}";
        return $"{relative.DisplayName}{legacyText} • {type}";
    }

    private sealed record PersonRow(Guid Id, string DisplayName, string LegacyNumber, string BirthDisplay, string DeathDisplay);

    private sealed record RelativeRow(Guid Id, string GivenName, string MiddleName, string Surname, string Suffix)
    {
        public string DisplayName => JoinName(GivenName, MiddleName, Surname, Suffix);
    }
}
