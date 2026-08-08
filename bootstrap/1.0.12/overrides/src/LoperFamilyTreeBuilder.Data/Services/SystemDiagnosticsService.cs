using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class SystemDiagnosticsService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    ArchiveConfigurationStore configurationStore,
    AdvancedBackupService backupService,
    ApplicationPaths applicationPaths)
{
    public async Task<SystemDiagnosticsReport> GetReportAsync(
        CancellationToken cancellationToken = default)
    {
        var checks = new List<SystemDiagnosticCheck>();
        var people = 0;
        var branches = 0;
        var parentChild = 0;
        var couples = 0;
        var legacyNumbers = 0;
        var duplicateLegacyNumbers = 0;
        var auditEvents = 0;
        var latestMigration = "None";

        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            checks.Add(new SystemDiagnosticCheck(
                "Database",
                "Database connection",
                canConnect,
                true,
                canConnect ? "SQL Server LocalDB responded successfully." : "The genealogy database could not be reached."));

            if (canConnect)
            {
                people = await db.People.CountAsync(cancellationToken);
                branches = await db.FamilyBranches.CountAsync(cancellationToken);
                parentChild = await db.ParentChildRelationships.CountAsync(cancellationToken);
                couples = await db.CoupleRelationships.CountAsync(cancellationToken);
                auditEvents = await db.AuditEvents.CountAsync(cancellationToken);

                var migrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                latestMigration = migrations.LastOrDefault() ?? "None";
                checks.Add(new SystemDiagnosticCheck(
                    "Database",
                    "Migration history",
                    migrations.Count > 0,
                    true,
                    migrations.Count > 0
                        ? $"{migrations.Count} migrations applied. Latest: {latestMigration}."
                        : "No Entity Framework migration history was found."));

                legacyNumbers = await db.PersonIdentifiers
                    .CountAsync(x => x.IdentifierType == PersonIdentifierType.LegacyNumber, cancellationToken);

                duplicateLegacyNumbers = await db.PersonIdentifiers
                    .Where(x => x.IdentifierType == PersonIdentifierType.LegacyNumber)
                    .GroupBy(x => x.Value)
                    .CountAsync(g => g.Count() > 1, cancellationToken);

                checks.Add(new SystemDiagnosticCheck(
                    "Preservation",
                    "Legacy Number uniqueness",
                    duplicateLegacyNumbers == 0,
                    true,
                    duplicateLegacyNumbers == 0
                        ? $"{legacyNumbers} protected Legacy Numbers checked with no duplicate historical values."
                        : $"{duplicateLegacyNumbers} duplicate Legacy Number value(s) require review. No values were changed."));
            }
        }

        var configuration = await configurationStore.LoadAsync(cancellationToken);
        var configurationReady = configuration is { IsComplete: true };
        checks.Add(new SystemDiagnosticCheck(
            "Storage",
            "Archive configuration",
            configurationReady,
            true,
            configurationReady
                ? "Primary archive and backup locations are configured."
                : "Primary archive and backup locations are not fully configured."));

        if (configurationReady)
        {
            AddDirectoryCheck(checks, "Primary archive", configuration!.PrimaryArchivePath, required: true);
            AddDirectoryCheck(checks, "Backup location", configuration.BackupPath, required: true);
        }

        var localDirectoriesReady = Directory.Exists(applicationPaths.DatabaseDirectory)
            && Directory.Exists(applicationPaths.ConfigurationDirectory)
            && Directory.Exists(applicationPaths.LogDirectory);
        checks.Add(new SystemDiagnosticCheck(
            "Storage",
            "Local application directories",
            localDirectoriesReady,
            true,
            localDirectoriesReady
                ? "Database, configuration, and log directories are present."
                : "One or more local application directories are missing."));

        var backups = await backupService.GetBackupsAsync(cancellationToken);
        checks.Add(new SystemDiagnosticCheck(
            "Recovery",
            "Managed backup catalog",
            backups.Count > 0,
            false,
            backups.Count > 0
                ? $"{backups.Count} managed backup(s) are cataloged. Latest: {backups.Max(x => x.CreatedUtc).ToLocalTime():g}."
                : "No managed backup has been created yet."));

        return new SystemDiagnosticsReport(
            ApplicationVersion: "1.0.12",
            GeneratedUtc: DateTimeOffset.UtcNow,
            People: people,
            FamilyBranches: branches,
            ParentChildRelationships: parentChild,
            CoupleRelationships: couples,
            LegacyNumbers: legacyNumbers,
            DuplicateLegacyNumbers: duplicateLegacyNumbers,
            AuditEvents: auditEvents,
            ManagedBackups: backups.Count,
            LatestAppliedMigration: latestMigration,
            Checks: checks);
    }

    private static void AddDirectoryCheck(
        ICollection<SystemDiagnosticCheck> checks,
        string name,
        string path,
        bool required)
    {
        var exists = !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        checks.Add(new SystemDiagnosticCheck(
            "Storage",
            name,
            exists,
            required,
            exists ? path : $"Path is unavailable: {path}"));
    }
}
