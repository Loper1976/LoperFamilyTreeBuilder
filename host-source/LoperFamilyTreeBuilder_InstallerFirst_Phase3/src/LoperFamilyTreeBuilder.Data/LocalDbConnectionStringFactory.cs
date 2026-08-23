using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LoperFamilyTreeBuilder.Data;

public sealed class LocalDbConnectionStringFactory
{
    private readonly ApplicationPaths _paths;

    public LocalDbConnectionStringFactory(ApplicationPaths paths)
    {
        _paths = paths;
    }

    public string Create()
    {
        _paths.EnsureLocalDirectories();

        var escapedFile = _paths.DatabaseFile.Replace(";", ";;");

        var configuredRoot = Environment.GetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT");
        var databaseName = "LoperFamilyTreeBuilder";
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(configuredRoot))))[..12];
            databaseName += "_" + hash;
        }

        return
            "Server=(localdb)\\MSSQLLocalDB;" +
            "Integrated Security=true;" +
            "TrustServerCertificate=true;" +
            $"AttachDbFilename={escapedFile};" +
            $"Database={databaseName};" +
            "MultipleActiveResultSets=true;";
    }
}
