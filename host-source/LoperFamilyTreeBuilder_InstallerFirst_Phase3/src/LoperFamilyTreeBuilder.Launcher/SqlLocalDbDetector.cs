using System.Diagnostics;

namespace LoperFamilyTreeBuilder.Launcher;

internal static class SqlLocalDbDetector
{
    public static bool IsInstalled()
    {
        var candidates = GetCandidateExecutables();

        foreach (var executable in candidates)
        {
            if (!File.Exists(executable))
                continue;

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "versions",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process is null)
                    continue;

                if (!process.WaitForExit(5000))
                {
                    process.Kill(entireProcessTree: true);
                    continue;
                }

                if (process.ExitCode == 0)
                    return true;
            }
            catch
            {
                // Try the next known SQL Server LocalDB path.
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCandidateExecutables()
    {
        var programFiles =
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        foreach (var majorVersion in new[] { "170", "160", "150", "140" })
        {
            yield return Path.Combine(
                programFiles,
                "Microsoft SQL Server",
                majorVersion,
                "Tools",
                "Binn",
                "SqlLocalDB.exe");
        }
    }
}
