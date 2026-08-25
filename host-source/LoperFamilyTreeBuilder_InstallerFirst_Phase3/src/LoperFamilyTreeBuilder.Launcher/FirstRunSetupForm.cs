using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using LoperFamilyTreeBuilder.Infrastructure.Storage;

namespace LoperFamilyTreeBuilder.Launcher;

internal sealed class FirstRunSetupForm : Form
{
    private readonly ArchiveConfigurationStore _configurationStore;
    private readonly StorageValidationService _storageValidationService;

    private readonly TextBox _archivePath = new();
    private readonly TextBox _backupPath = new();
    private readonly Label _status = new();
    private readonly Button _archiveBrowseButton = new();
    private readonly Button _backupBrowseButton = new();
    private readonly Button _continueButton = new();

    public FirstRunSetupForm(
        ArchiveConfigurationStore configurationStore,
        StorageValidationService storageValidationService,
        ArchiveConfiguration? existingConfiguration)
    {
        _configurationStore = configurationStore;
        _storageValidationService = storageValidationService;

        Text = "Loper Family Tree Builder Setup";
        Width = 760;
        Height = 430;
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildLayout();

        if (existingConfiguration is not null)
        {
            _archivePath.Text = existingConfiguration.PrimaryArchivePath;
            _backupPath.Text = existingConfiguration.BackupPath;
        }
    }

    public bool ConfigurationSaved { get; private set; }

    private void BuildLayout()
    {
        var title = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Loper Family Tree Builder",
            Left = 28,
            Top = 24
        };

        var description = new Label
        {
            AutoSize = false,
            Width = 680,
            Height = 50,
            Left = 28,
            Top = 55,
            Text = "Choose where the permanent family archive and backup copies will be stored. " +
                   "The genealogy database remains on reliable local Windows storage."
        };

        var archiveLabel = new Label
        {
            AutoSize = true,
            Text = "Primary family archive",
            Left = 28,
            Top = 125
        };

        _archivePath.SetBounds(28, 150, 590, 30);

        _archiveBrowseButton.Text = "Browse...";
        _archiveBrowseButton.SetBounds(630, 148, 85, 28);
        _archiveBrowseButton.Click += async (_, _) =>
            await BrowseForFolderAsync(
                _archivePath,
                _archiveBrowseButton,
                "primary family archive");

        var backupLabel = new Label
        {
            AutoSize = true,
            Text = "Backup location",
            Left = 28,
            Top = 205
        };

        _backupPath.SetBounds(28, 230, 590, 30);

        _backupBrowseButton.Text = "Browse...";
        _backupBrowseButton.SetBounds(630, 228, 85, 28);
        _backupBrowseButton.Click += async (_, _) =>
            await BrowseForFolderAsync(
                _backupPath,
                _backupBrowseButton,
                "backup location");

        _status.SetBounds(28, 280, 680, 45);
        _status.Text = "Both folders will be tested before setup continues.";

        _continueButton.Text = "Save and Open Family Tree Builder";
        _continueButton.SetBounds(430, 330, 285, 36);
        _continueButton.Click += async (_, _) => await SaveConfigurationAsync();

        var cancelButton = new Button
        {
            Text = "Cancel",
            Left = 330,
            Top = 330,
            Width = 90
        };
        cancelButton.Click += (_, _) => Close();

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(archiveLabel);
        Controls.Add(_archivePath);
        Controls.Add(_archiveBrowseButton);
        Controls.Add(backupLabel);
        Controls.Add(_backupPath);
        Controls.Add(_backupBrowseButton);
        Controls.Add(_status);
        Controls.Add(cancelButton);
        Controls.Add(_continueButton);
    }

    private async Task BrowseForFolderAsync(
        TextBox target,
        Button browseButton,
        string folderDescription)
    {
        browseButton.Enabled = false;
        _status.Text = $"Opening the {folderDescription} folder browser...";

        try
        {
            var initialDirectory = string.IsNullOrWhiteSpace(target.Text)
                ? null
                : target.Text;

            var selectedPath = await ShowFolderBrowserOnStaThreadAsync(
                folderDescription,
                initialDirectory);

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                target.Text = selectedPath;
                _status.Text =
                    $"Selected {folderDescription}: {selectedPath}";
            }
            else
            {
                _status.Text =
                    "Folder selection was canceled. Both folders will be tested before setup continues.";
            }
        }
        catch (Exception ex)
        {
            _status.Text =
                $"The folder browser could not be opened: {ex.Message}";
        }
        finally
        {
            browseButton.Enabled = true;
        }
    }

    private static Task<string?> ShowFolderBrowserOnStaThreadAsync(
        string folderDescription,
        string? initialDirectory)
    {
        var completion = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var browserThread = new Thread(() =>
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = $"Select the {folderDescription} folder for Loper Family Tree Builder",
                    ShowNewFolderButton = true,
                    UseDescriptionForTitle = true
                };

                // Do not call Directory.Exists here. Checking a disconnected
                // mapped or network drive on the UI thread can freeze the setup
                // window before the folder browser is even shown.
                if (!string.IsNullOrWhiteSpace(initialDirectory))
                {
                    dialog.InitialDirectory = initialDirectory;
                }

                var result = dialog.ShowDialog();

                completion.TrySetResult(
                    result == DialogResult.OK
                        ? dialog.SelectedPath
                        : null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "LoperFamilyTreeBuilder.FolderBrowser"
        };

        browserThread.SetApartmentState(ApartmentState.STA);
        browserThread.Start();

        return completion.Task;
    }

    private async Task SaveConfigurationAsync()
    {
        _continueButton.Enabled = false;
        _archiveBrowseButton.Enabled = false;
        _backupBrowseButton.Enabled = false;
        _status.Text = "Testing the primary archive folder...";

        try
        {
            var archiveResult =
                await _storageValidationService.ValidateWritableFolderAsync(
                    _archivePath.Text);

            if (!archiveResult.IsValid)
            {
                _status.Text =
                    $"Primary archive is not available: {archiveResult.Message}";
                return;
            }

            _status.Text = "Testing the backup folder...";

            var backupResult =
                await _storageValidationService.ValidateWritableFolderAsync(
                    _backupPath.Text);

            if (!backupResult.IsValid)
            {
                _status.Text =
                    $"Backup location is not available: {backupResult.Message}";
                return;
            }

            var archiveFullPath = Path.GetFullPath(_archivePath.Text);
            var backupFullPath = Path.GetFullPath(_backupPath.Text);

            if (string.Equals(
                archiveFullPath.TrimEnd(Path.DirectorySeparatorChar),
                backupFullPath.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            {
                _status.Text =
                    "The primary archive and backup must be different folders.";
                return;
            }

            var configuration = new ArchiveConfiguration
            {
                PrimaryArchivePath = archiveFullPath,
                BackupPath = backupFullPath,
                ConfiguredUtc = DateTimeOffset.UtcNow
            };

            await _configurationStore.SaveAsync(configuration);

            ConfigurationSaved = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _status.Text = $"Setup could not be saved: {ex.Message}";
        }
        finally
        {
            _continueButton.Enabled = true;
            _archiveBrowseButton.Enabled = true;
            _backupBrowseButton.Enabled = true;
        }
    }
}
