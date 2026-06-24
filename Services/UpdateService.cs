using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MortuaryApp.Services;

public static class UpdateService
{
    private static readonly HttpClient Http = new();
    private static string _owner = "rajibia";
    private static string _repo = "mortuary-app";

    public static string GitHubRepo
    {
        get => $"{_owner}/{_repo}";
        set
        {
            var parts = value.Split('/');
            if (parts.Length == 2)
            {
                _owner = parts[0];
                _repo = parts[1];
            }
        }
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("MortuaryApp-Updater/1.0");

            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var version = tagName.TrimStart('v');

            if (!Version.TryParse(version, out var latestVersion))
                return null;

            if (latestVersion <= CurrentVersion)
                return null;

            var assets = root.GetProperty("assets");
            if (assets.GetArrayLength() == 0) return null;

            var downloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? "";

            return new UpdateInfo
            {
                LatestVersion = latestVersion,
                DownloadUrl = downloadUrl,
                ReleaseNotes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : ""
            };
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> DownloadUpdateAsync(string url)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "MortuaryUpdate");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, "update.zip");
            var resp = await Http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            await using var fs = File.Create(zipPath);
            await resp.Content.CopyToAsync(fs);

            return zipPath;
        }
        catch
        {
            return null;
        }
    }

    public static void InstallUpdate(string zipPath)
    {
        try
        {
            var appDir = AppContext.BaseDirectory;
            var tempDir = Path.Combine(Path.GetTempPath(), "MortuaryUpdate");
            var extractedDir = Path.Combine(tempDir, "extracted");
            var scriptPath = Path.Combine(tempDir, "install.cmd");

            if (Directory.Exists(extractedDir))
                Directory.Delete(extractedDir, true);
            ZipFile.ExtractToDirectory(zipPath, extractedDir);

            var pid = Environment.ProcessId;
            var script = $@"@echo off
:wait
tasklist /FI ""PID eq {pid}"" 2>nul | find ""{pid}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait
)
timeout /t 1 /nobreak >nul
echo Installing update...
xcopy /y /e /q ""{extractedDir}\*"" ""{appDir}""
start """" ""{appDir}\MortuaryApp.exe""
del ""%~f0""
";
            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo("cmd.exe", $"/c \"{scriptPath}\"")
            {
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}", "Update Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class UpdateInfo
{
    public Version LatestVersion { get; set; } = new();
    public string DownloadUrl { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
}
