using System.Diagnostics;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using LoperFamilyTreeBuilder.Infrastructure.Storage;

namespace LoperFamilyTreeBuilder.Launcher;

internal static class Program
{
    private const string LocalUrl = "http://127.0.0.1:5177";
    private const string HealthUrl = LocalUrl + "/health";

    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: @"Local\LoperFamilyTreeBuilder.Launcher",
            createdNew: out var isFirstInstance);

        if (!isFirstInstance)
        {
            OpenBrowser(LocalUrl);
            return;
        }

        if (!SqlLocalDbDetector.IsInstalled())
        {
            MessageBox.Show(
                "The SQL Server LocalDB database component is missing. " +
                "Run Repair on Loper Family Tree Builder so Setup can restore the required database component.",
                "Loper Family Tree Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var paths = new ApplicationPaths();
        paths.EnsureLocalDirectories();

        var configurationStore = new ArchiveConfigurationStore(paths);
        var storageValidationService = new StorageValidationService();

        var configuration = await configurationStore.LoadAsync();

        if (configuration is null || !configuration.IsComplete)
        {
            using var setupForm = new FirstRunSetupForm(
                configurationStore,
                storageValidationService,
                configuration);

            Application.Run(setupForm);

            if (!setupForm.ConfigurationSaved)
            {
                return;
            }

            configuration = await configurationStore.LoadAsync();
        }

        if (configuration is null)
        {
            return;
        }

        var archiveResult =
            await storageValidationService.ValidateWritableFolderAsync(
                configuration.PrimaryArchivePath);

        var backupResult =
            await storageValidationService.ValidateWritableFolderAsync(
                configuration.BackupPath);

        if (!archiveResult.IsValid || !backupResult.IsValid)
        {
            using var repairForm = new FirstRunSetupForm(
                configurationStore,
                storageValidationService,
                configuration);

            Application.Run(repairForm);

            if (!repairForm.ConfigurationSaved)
            {
                return;
            }
        }

        if (await IsHealthyAsync())
        {
            OpenBrowser(LocalUrl);
            return;
        }

        var appDirectory = AppContext.BaseDirectory;
        var webExecutable = Path.Combine(
            appDirectory,
            "Web",
            "LoperFamilyTreeBuilder.Web.exe");

        if (!File.Exists(webExecutable))
        {
            MessageBox.Show(
                "The Loper Family Tree Builder web application was not found. Run Repair from Windows Installed Apps.",
                "Loper Family Tree Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using var webProcess = StartWebApplication(webExecutable);

        if (webProcess is null)
        {
            MessageBox.Show(
                "Loper Family Tree Builder could not be started.",
                "Loper Family Tree Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!await WaitForHealthAsync(TimeSpan.FromSeconds(60)))
        {
            TryStop(webProcess);

            MessageBox.Show(
                "Loper Family Tree Builder did not become ready. The local database component may need repair.",
                "Loper Family Tree Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        OpenBrowser(LocalUrl);

        await webProcess.WaitForExitAsync();
    }

    private static Process? StartWebApplication(string webExecutable)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = webExecutable,
            Arguments = "--urls http://127.0.0.1:5177",
            WorkingDirectory = Path.GetDirectoryName(webExecutable)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        return Process.Start(startInfo);
    }

    private static async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(1)
            };

            using var response = await client.GetAsync(HealthUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> WaitForHealthAsync(TimeSpan timeout)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(HealthUrl);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Application is still starting.
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static void TryStop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort shutdown only.
        }
    }
}
