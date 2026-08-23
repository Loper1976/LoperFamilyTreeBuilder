namespace LoperFamilyTreeBuilder.Infrastructure.Configuration;

public sealed class ApplicationPaths
{
    private const string ProductFolderName = "Loper Family Tree Builder";

    public string LocalApplicationRoot =>
        Environment.GetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT") is { Length: > 0 } configuredRoot
            ? Path.GetFullPath(configuredRoot)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductFolderName);

    public string ConfigurationDirectory =>
        Path.Combine(LocalApplicationRoot, "Config");

    public string ConfigurationFile =>
        Path.Combine(ConfigurationDirectory, "archive-settings.json");

    public string DatabaseDirectory =>
        Path.Combine(LocalApplicationRoot, "Database");

    public string DatabaseFile =>
        Path.Combine(DatabaseDirectory, "LoperFamilyTreeBuilder.mdf");

    public string LogDirectory =>
        Path.Combine(LocalApplicationRoot, "Logs");

    public string ResearchDirectory => Path.Combine(LocalApplicationRoot, "Research");
    public string BackupDirectory => Path.Combine(LocalApplicationRoot, "Backups");

    public void EnsureLocalDirectories()
    {
        Directory.CreateDirectory(ConfigurationDirectory);
        Directory.CreateDirectory(DatabaseDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(ResearchDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }
}
