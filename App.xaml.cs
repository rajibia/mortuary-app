using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;
using MortuaryApp.Services;
using PdfSharp.Fonts;

namespace MortuaryApp;

public partial class App : Application
{
    public static User? CurrentUser { get; set; }
    public static bool IsDarkTheme { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        GlobalFontSettings.FontResolver = new CustomFontResolver();
        DbInitializer.Initialize();

        var ghToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(ghToken))
            UpdateService.Token = ghToken;

        if (!IsLicensed())
        {
            var activation = new LicenseActivationWindow();
            if (activation.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        var login = new LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        CurrentUser = login.AuthenticatedUser;
        _ = Services.PermissionService.LoadAsync();

        LoadThemePreference();

        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open main window: {ex.Message}\n\n{ex.GetType()}\n\n{ex.StackTrace}", "Fatal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplyTheme(IsDarkTheme);
        SaveThemePreference();
    }

    private static void LoadThemePreference()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var setting = db.Settings.FirstOrDefault(s => s.Key == "theme");
            IsDarkTheme = setting?.Value == "dark";
            ApplyTheme(IsDarkTheme);
        }
        catch
        {
            IsDarkTheme = false;
        }
    }

    private static void SaveThemePreference()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var setting = db.Settings.FirstOrDefault(s => s.Key == "theme");
            if (setting != null)
            {
                setting.Value = IsDarkTheme ? "dark" : "light";
                setting.UpdatedAt = DateTime.Now;
            }
            else
            {
                db.Settings.Add(new Setting { Key = "theme", Value = IsDarkTheme ? "dark" : "light" });
            }
            db.SaveChanges();
        }
        catch { }
    }

    private static void ApplyTheme(bool dark)
    {
        var bundle = Current.Resources.MergedDictionaries
            .OfType<BundledTheme>()
            .FirstOrDefault();
        if (bundle != null)
            bundle.BaseTheme = dark ? BaseTheme.Dark : BaseTheme.Light;

        var dict = Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.ToString().Contains("CustomStyles") == true);
        if (dict == null) return;

        if (dark)
        {
            SetBrush(dict, "ContentBgBrush", "#0F172A");
            SetBrush(dict, "CardBgBrush", "#1E293B");
            SetBrush(dict, "BorderBrushTheme", "#334155");
            SetBrush(dict, "TextPrimaryBrush", "#F1F5F9");
            SetBrush(dict, "TextSecondaryBrush", "#94A3B8");
            SetBrush(dict, "TextMutedBrush", "#64748B");
        }
        else
        {
            SetBrush(dict, "ContentBgBrush", "#F1F5F9");
            SetBrush(dict, "CardBgBrush", "#FFFFFF");
            SetBrush(dict, "BorderBrushTheme", "#E2E8F0");
            SetBrush(dict, "TextPrimaryBrush", "#0F172A");
            SetBrush(dict, "TextSecondaryBrush", "#64748B");
            SetBrush(dict, "TextMutedBrush", "#94A3B8");
        }
    }

    private static void SetBrush(ResourceDictionary dict, string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        dict[key] = new SolidColorBrush(color);
    }

    private static bool IsLicensed()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var key = db.Settings.FirstOrDefault(s => s.Key == "license_key")?.Value;
            return !string.IsNullOrEmpty(key) && LicenseService.ValidateLicense(key);
        }
        catch { return false; }
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info != null)
            {
                var main = Current.MainWindow;
                if (main != null)
                {
                    main.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Version {info.LatestVersion} is available!\nGo to Settings → Updates to install.",
                            "Update Available",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
        }
        catch { }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Unexpected error: {e.Exception.Message}", "Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
