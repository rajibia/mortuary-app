using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MortuaryApp.Services;

public static class UpdateService
{
    private static readonly HttpClient Http = new();
    private static string _owner = "rajibia";
    private static string _repo = "mortuary-app";
    private static string _token = "";

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

    public static string Token
    {
        get => _token;
        set => _token = value ?? "";
    }

    public static Version CurrentVersion
    {
        get
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var fvi = FileVersionInfo.GetVersionInfo(location);
            return new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
        }
    }

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("MortuaryApp-Updater/1.0");
            if (!string.IsNullOrEmpty(_token))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

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

    public static async Task<string?> DownloadUpdateAsync(string url, IProgress<double>? progress = null)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "MortuaryUpdate");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, "update.zip");
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(_token))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength ?? -1L;
            await using var fs = File.Create(zipPath);
            await using var stream = await resp.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            long read = 0;
            int bytes;
            while ((bytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytes);
                read += bytes;
                if (progress != null && total > 0)
                    progress.Report((double)read / total);
            }

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
            var sysTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MortuaryUpdate");
            var sysExtracted = Path.Combine(sysTemp, "extracted");

            Directory.CreateDirectory(sysTemp);
            if (Directory.Exists(sysExtracted))
                Directory.Delete(sysExtracted, true);

            ZipFile.ExtractToDirectory(zipPath, sysExtracted);

            var pid = Environment.ProcessId;
            var logPath = Path.Combine(sysTemp, "install.log");
            var script = $@"@echo off
echo Update started at %DATE% %TIME% > ""{logPath}""
:wait
tasklist /FI ""PID eq {pid}"" 2>nul | find ""{pid}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait
)
timeout /t 1 /nobreak >nul
echo Installing update... >> ""{logPath}""
robocopy ""{sysExtracted}"" ""{appDir}"" /E /IS /IT /R:3 /W:2 /NP >> ""{logPath}""
echo Copy exit code: %ERRORLEVEL% >> ""{logPath}""
if %ERRORLEVEL% gtr 7 (
    echo robocopy FAILED with exit code %ERRORLEVEL% >> ""{logPath}""
    pause
    exit /b %ERRORLEVEL%
)
start """" ""{appDir}\MortuaryApp.exe""
echo App launched >> ""{logPath}""
del ""%~f0""
";
            File.WriteAllText(Path.Combine(sysTemp, "install.cmd"), script);

            var psi = new ProcessStartInfo("cmd.exe", $"/c \"{Path.Combine(sysTemp, "install.cmd")}\"")
            {
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(psi);
            Thread.Sleep(2000);
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
