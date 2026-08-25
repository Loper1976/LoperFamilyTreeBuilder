using System.Data;
using Microsoft.EntityFrameworkCore;
using ResearchAgent.Core.Integration;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class HostDatabaseBackupService(
    IDbContextFactory<FamilyTreeDbContext> contexts,
    ApplicationPaths paths) : IHostBackupGate
{
    public async Task<string> CreatePreChangeBackupAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Backup reason is required.", nameof(reason));
        Directory.CreateDirectory(paths.BackupDirectory);
        var path = Path.Combine(Path.GetFullPath(paths.BackupDirectory), $"LoperFamilyTreeBuilder-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.bak");
        await ExecuteAsync("BACKUP DATABASE [{0}] TO DISK = @path WITH COPY_ONLY, CHECKSUM, INIT", path, cancellationToken);
        return path;
    }

    public Task VerifyBackupAsync(string backupReference, CancellationToken cancellationToken = default)
    {
        if (!Path.IsPathFullyQualified(backupReference) || !File.Exists(backupReference))
            throw new InvalidOperationException("The backup file does not exist.");
        return ExecuteAsync("RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM", backupReference, cancellationToken);
    }

    public async Task RestoreAndQueryTestCopyAsync(string backupReference, CancellationToken cancellationToken = default)
    {
        await VerifyBackupAsync(backupReference, cancellationToken);
        var suffix = Guid.NewGuid().ToString("N");
        var databaseName = "LFTB_RestoreTest_" + suffix;
        var dataFile = Path.Combine(paths.BackupDirectory, databaseName + ".mdf");
        var logFile = Path.Combine(paths.BackupDirectory, databaseName + "_log.ldf");
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var restore = connection.CreateCommand();
            restore.CommandTimeout = 300;
            restore.CommandText = $"USE [master]; RESTORE DATABASE [{databaseName}] FROM DISK = @backup WITH MOVE N'LoperFamilyTreeBuilder' TO @data, MOVE N'LoperFamilyTreeBuilder_log' TO @log, CHECKSUM, RECOVERY; SELECT COUNT_BIG(*) FROM [{databaseName}].[dbo].[People];";
            Add(restore, "@backup", backupReference);
            Add(restore, "@data", dataFile);
            Add(restore, "@log", logFile);
            _ = await restore.ExecuteScalarAsync(cancellationToken);
        }
        finally
        {
            await using var drop = connection.CreateCommand();
            drop.CommandTimeout = 300;
            drop.CommandText = $"USE [master]; IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END";
            await drop.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task ExecuteAsync(string commandTemplate, string path, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var nameCommand = connection.CreateCommand();
        nameCommand.CommandText = "SELECT DB_NAME()";
        var databaseName = (string?)await nameCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Database name could not be resolved.");
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 300;
        command.CommandText = string.Format(commandTemplate, databaseName.Replace("]", "]]"));
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@path";
        parameter.DbType = DbType.String;
        parameter.Value = path;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
