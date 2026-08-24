using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace LoperFamilyDataUploader;

public sealed class UploaderForm : Form
{
    private const string RemoteRepo = "https://github.com/Loper1976/LoperFamilyTreeData.git";
    private const long NormalGitLimit = 95L * 1024 * 1024;
    private const long LfsSafetyLimit = 2L * 1024 * 1024 * 1024;

    private readonly TextBox _source = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly Button _choose = new() { Text = "Choose Folder", AutoSize = true };
    private readonly Button _scan = new() { Text = "Scan & Classify", AutoSize = true, Enabled = false };
    private readonly Button _upload = new() { Text = "Upload to Family Archive", AutoSize = true, Enabled = false };
    private readonly Label _summary = new() { AutoSize = true, Text = "Choose a folder containing your family-tree files." };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill };
    private readonly TextBox _log = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private List<ScannedFile> _files = [];

    public UploaderForm()
    {
        Text = "Loper Family Data Uploader v2";
        Width = 980; Height = 680; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(760, 520);
        var top = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var sourceLabel = new Label { Text = "Source folder", AutoSize = true }; top.Controls.Add(sourceLabel, 0, 0); top.SetColumnSpan(sourceLabel, 2);
        top.Controls.Add(_source, 0, 1); top.Controls.Add(_choose, 1, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16, 0, 16, 8) }; actions.Controls.Add(_scan); actions.Controls.Add(_upload);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(16) };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(new Label { Text = "Preserves originals, hashes every file, detects duplicates, and creates non-destructive classification staging for review.", AutoSize = true }, 0, 0);
        body.Controls.Add(_summary, 0, 1); body.Controls.Add(_progress, 0, 2); body.Controls.Add(_log, 0, 3);
        Controls.Add(body); Controls.Add(actions); Controls.Add(top);
        _choose.Click += (_, _) => ChooseFolder(); _scan.Click += async (_, _) => await ScanAsync(); _upload.Click += async (_, _) => await UploadAsync();
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select the folder containing family-tree data", ShowNewFolderButton = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _source.Text = dialog.SelectedPath; _scan.Enabled = true; _upload.Enabled = false; _files.Clear(); _summary.Text = "Folder selected. Click Scan & Classify."; _log.Clear();
    }

    private async Task ScanAsync()
    {
        SetBusy(true);
        try
        {
            _files = []; _log.Clear(); var paths = Directory.EnumerateFiles(_source.Text, "*", SearchOption.AllDirectories).ToList();
            _progress.Maximum = Math.Max(1, paths.Count); _progress.Value = 0; long bytes = 0; int oversize = 0; var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i]; var info = new FileInfo(path); var relative = Path.GetRelativePath(_source.Text, path).Replace('\\', '/');
                string sha; await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true)) { using var hasher = SHA256.Create(); sha = Convert.ToHexString(await hasher.ComputeHashAsync(stream)).ToLowerInvariant(); }
                hashes.TryGetValue(sha, out var duplicateOf); hashes.TryAdd(sha, relative); var tooLarge = info.Length > LfsSafetyLimit; if (tooLarge) oversize++; bytes += info.Length;
                var classification = Classify(relative);
                _files.Add(new ScannedFile(relative, info.Length, sha, info.Length > NormalGitLimit, tooLarge, duplicateOf, classification.Category, classification.Confidence, classification.Reason)); _progress.Value = i + 1;
            }
            var duplicateCount = _files.Count(f => f.DuplicateOf is not null); var classes = string.Join(", ", _files.GroupBy(f => f.Category).OrderByDescending(g => g.Count()).Take(6).Select(g => $"{g.Key}:{g.Count()}"));
            _summary.Text = $"{_files.Count:N0} files | {FormatBytes(bytes)} | {_files.Count(f => f.UseLfs):N0} LFS | {duplicateCount:N0} duplicates | {oversize:N0} over 2 GB";
            Append($"Scan complete: {_summary.Text}"); Append("Classification preview: " + classes);
            _upload.Enabled = oversize == 0 && _files.Count > 0; if (oversize > 0) Append("Upload blocked until files over 2 GB are removed or split.");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error); Append("ERROR: " + ex); }
        finally { SetBusy(false); }
    }

    private static (string Category, double Confidence, string Reason) Classify(string relative)
    {
        var ext = Path.GetExtension(relative).ToLowerInvariant(); var text = relative.ToLowerInvariant();
        if (ext is ".ged" or ".gedcom") return ("GEDCOM", .99, "GEDCOM file extension");
        if (text.Contains("census")) return ("Census", .90, "Filename/path contains census");
        if (text.Contains("birth") || text.Contains("baptis")) return ("Vital-Birth", .85, "Filename/path indicates birth or baptism");
        if (text.Contains("marriage") || text.Contains("wedding")) return ("Vital-Marriage", .85, "Filename/path indicates marriage");
        if (text.Contains("death") || text.Contains("obit") || text.Contains("cemet") || text.Contains("grave")) return ("Vital-Death-Cemetery", .85, "Filename/path indicates death, obituary, cemetery, or grave");
        if (text.Contains("military") || text.Contains("navy") || text.Contains("army") || text.Contains("marine") || text.Contains("air force") || text.Contains("dd214")) return ("Military", .85, "Filename/path indicates military record");
        if (text.Contains("newspaper") || text.Contains("article") || text.Contains("clipping")) return ("Newspaper", .80, "Filename/path indicates newspaper material");
        if (ext is ".jpg" or ".jpeg" or ".png" or ".heic" or ".tif" or ".tiff" or ".webp" or ".bmp") return ("Photo-Image", .75, "Image file extension");
        if (ext is ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf") return ("Document-Unclassified", .55, "Document file extension; requires review");
        if (ext is ".zip" or ".7z" or ".rar") return ("Archive", .70, "Archive file extension");
        return ("Other-Unclassified", .40, "No high-confidence rule matched");
    }

    private async Task UploadAsync()
    {
        SetBusy(true);
        try
        {
            var repo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoperFamilyDataUploader", "LoperFamilyTreeData"); Directory.CreateDirectory(Path.GetDirectoryName(repo)!);
            Append("Checking Git..."); await RunAsync("git", ["--version"]);
            if (!Directory.Exists(Path.Combine(repo, ".git"))) { Append("Creating local working copy..."); await RunAsync("git", ["clone", RemoteRepo, repo]); }
            else { Append("Refreshing local working copy..."); await RunAsync("git", ["-C", repo, "pull", "--ff-only", "origin", "main"]); }
            await RunAsync("git", ["-C", repo, "config", "user.name", "Phil Loper"]); await RunAsync("git", ["-C", repo, "config", "user.email", "phil@loper.com"]);
            Append("Checking Git LFS..."); await RunAsync("git", ["-C", repo, "lfs", "install", "--local"]);
            var batchId = DateTime.Now.ToString("yyyyMMdd-HHmmss"); var batchRoot = Path.Combine(repo, "INBOX", batchId); var originals = Path.Combine(batchRoot, "originals"); Directory.CreateDirectory(originals);
            _progress.Maximum = Math.Max(1, _files.Count); _progress.Value = 0;
            for (var i = 0; i < _files.Count; i++)
            {
                var file = _files[i]; var sourcePath = Path.Combine(_source.Text, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)); var destPath = Path.Combine(originals, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(destPath)!); File.Copy(sourcePath, destPath, true);
                if (file.UseLfs) { var repoRelative = Path.GetRelativePath(repo, destPath).Replace('\\', '/'); await RunAsync("git", ["-C", repo, "lfs", "track", repoRelative]); } _progress.Value = i + 1;
            }
            var manifest = new UploadManifest(batchId, DateTimeOffset.Now, _files.Count, _files.Sum(f => f.Size), "Loper Family Data Uploader v2", _files);
            await File.WriteAllTextAsync(Path.Combine(batchRoot, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            var staging = _files.Select(f => new { f.RelativePath, f.Sha256, SuggestedCategory = f.Category, f.ClassificationConfidence, f.ClassificationReason, ReviewStatus = "pending", SuggestedLegacyNumber = (string?)null, SuggestedPerson = (string?)null }).ToList();
            await File.WriteAllTextAsync(Path.Combine(batchRoot, "classification-staging.json"), JsonSerializer.Serialize(staging, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(batchRoot, "README.md"), $"# Import batch {batchId}\n\nUploader v2 preserved {_files.Count:N0} originals. `manifest.json` contains hashes. `classification-staging.json` contains non-destructive suggested classifications for review. Originals are never moved or renamed by classification.\n");
            Append("Staging originals, manifest, and classification suggestions..."); await RunAsync("git", ["-C", repo, "add", $"INBOX/{batchId}"]); if (File.Exists(Path.Combine(repo, ".gitattributes"))) await RunAsync("git", ["-C", repo, "add", ".gitattributes"]);
            await RunAsync("git", ["-C", repo, "commit", "-m", $"Import family data batch {batchId} with classification staging"]); Append("Uploading to private family archive..."); await RunAsync("git", ["-C", repo, "push", "origin", "main"]);
            Append($"UPLOAD COMPLETE. Batch: {batchId}"); MessageBox.Show(this, $"Upload complete.\n\nBatch: {batchId}\nFiles: {_files.Count:N0}\n\nOriginals were preserved and classification suggestions were staged for review.", "Upload complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Append("ERROR: " + ex.Message); MessageBox.Show(this, ex.Message + "\n\nNothing is deleted from your source folder.", "Upload failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }

    private async Task RunAsync(string fileName, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true }; foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Unable to start {fileName}."); var stdout = await process.StandardOutput.ReadToEndAsync(); var stderr = await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync();
        if (!string.IsNullOrWhiteSpace(stdout)) Append(stdout.Trim()); if (!string.IsNullOrWhiteSpace(stderr)) Append(stderr.Trim()); if (process.ExitCode != 0) throw new InvalidOperationException($"{fileName} exited with code {process.ExitCode}. {stderr}".Trim());
    }

    private void SetBusy(bool busy) { UseWaitCursor = busy; _choose.Enabled = !busy; _scan.Enabled = !busy && !string.IsNullOrWhiteSpace(_source.Text); if (busy) _upload.Enabled = false; else if (_files.Count > 0 && !_files.Any(f => f.TooLarge)) _upload.Enabled = true; }
    private void Append(string text) { if (InvokeRequired) { BeginInvoke(new Action(() => Append(text))); return; } _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}"); }
    private static string FormatBytes(long bytes) { string[] suffix = ["B", "KB", "MB", "GB", "TB"]; double value = bytes; var i = 0; while (value >= 1024 && i < suffix.Length - 1) { value /= 1024; i++; } return $"{value:0.##} {suffix[i]}"; }
}

public sealed record ScannedFile(string RelativePath, long Size, string Sha256, bool UseLfs, bool TooLarge, string? DuplicateOf, string Category, double ClassificationConfidence, string ClassificationReason);
public sealed record UploadManifest(string BatchId, DateTimeOffset CreatedAt, int FileCount, long TotalBytes, string CreatedBy, IReadOnlyList<ScannedFile> Files);
