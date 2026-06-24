using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class SettingsPage : UserControl
{
    private readonly AuditService _audit = new();

        public SettingsPage()
        {
            InitializeComponent();
            EmailHelper.WireValidation(TxtOrgEmail);
            _ = LoadSettingsAsync();
        }

    private async Task LoadSettingsAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            var canEdit = PermissionService.Has("actions.settings.edit");
            PermPanel.IsEnabled = canEdit;
            BtnSaveId.IsEnabled = canEdit;
            BtnSaveOrg.IsEnabled = canEdit;
            BtnSaveSmtp.IsEnabled = canEdit;
            BtnAddHoliday.IsEnabled = canEdit;
            BtnAddRole.IsEnabled = canEdit;

            var idSetting = await db.IdSettings.FirstOrDefaultAsync(i => i.Scope == "mortuary_number");
            if (idSetting != null)
            {
                TxtPrefix.Text = idSetting.Prefix;
                TxtDigits.Text = idSetting.Digits.ToString();
            }

            DgHolidays.ItemsSource = await db.PublicHolidays.OrderBy(h => h.Date).ToListAsync();

            TxtOrgName.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgName"))?.Value ?? "";
            TxtOrgPhone.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgPhone"))?.Value ?? "";
            TxtOrgEmail.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgEmail"))?.Value ?? "";
            TxtOrgAddress.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgAddress"))?.Value ?? "";
            TxtOrgLogoPath.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgLogoPath"))?.Value ?? "";
            TxtAppName.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "AppName"))?.Value ?? "Msoft Mortuary Pro";

            var rpSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "RevenuePct");
            CboRevenuePct.SelectedValue = rpSetting?.Value ?? "100";
            RevenuePctCard.Visibility = Visibility.Visible;

            TxtSmtpHost.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpHost"))?.Value ?? "";
            TxtSmtpPort.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpPort"))?.Value ?? "587";
            TxtSmtpUsername.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpUsername"))?.Value ?? "";
            TxtSmtpFrom.Text = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpFromEmail"))?.Value ?? "";

            DgRoles.ItemsSource = await db.Roles.OrderBy(r => r.Name).ToListAsync();

            TxtUpdateStatus.Text = $"v{UpdateService.CurrentVersion}";
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnAddHoliday_Click(object sender, RoutedEventArgs e)
    {
        if (DpHoliday.SelectedDate == null || string.IsNullOrWhiteSpace(TxtHolidayName.Text))
        {
            ToastHelper.ShowWarning("Select a date and enter a name.");
            return;
        }

        using var db = new MortuaryDbContext();
        db.PublicHolidays.Add(new PublicHoliday { Date = DpHoliday.SelectedDate.Value, Name = TxtHolidayName.Text.Trim() });
        await db.SaveChangesAsync();
        _audit.Log("Settings", "Add Holiday", $"Added holiday {TxtHolidayName.Text}");
        await LoadSettingsAsync();
        TxtHolidayName.Clear();
    }

    private async void BtnSaveId_Click(object sender, RoutedEventArgs e)
    {
        using var db = new MortuaryDbContext();
        var idSetting = await db.IdSettings.FirstOrDefaultAsync(i => i.Scope == "mortuary_number");
        if (idSetting == null)
        {
            idSetting = new IdSetting { Scope = "mortuary_number" };
            db.IdSettings.Add(idSetting);
        }

        idSetting.Prefix = TxtPrefix.Text.Trim().ToUpper();
        idSetting.Digits = int.TryParse(TxtDigits.Text, out var d) ? d : 4;
        await db.SaveChangesAsync();
        _audit.Log("Settings", "Update", "ID number settings updated");
        ToastHelper.ShowSuccess("ID settings saved.");
    }

    private void BtnBrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Organization Logo",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
            TxtOrgLogoPath.Text = dlg.FileName;
    }

    private async void BtnSaveOrg_Click(object sender, RoutedEventArgs e)
    {
        var orgEmail = TxtOrgEmail.Text.Trim();

        if (!string.IsNullOrEmpty(orgEmail) && !EmailHelper.IsValidEmail(orgEmail))
        {
            ToastHelper.ShowWarning("Enter a valid email address.");
            return;
        }

        try
        {
            using var db = new MortuaryDbContext();
            await UpsertSettingAsync(db, "OrgName", TxtOrgName.Text.Trim());
            await UpsertSettingAsync(db, "OrgPhone", TxtOrgPhone.FullNumber);
            await UpsertSettingAsync(db, "OrgEmail", orgEmail);
            await UpsertSettingAsync(db, "OrgAddress", TxtOrgAddress.Text.Trim());
            await UpsertSettingAsync(db, "OrgLogoPath", TxtOrgLogoPath.Text.Trim());
            await UpsertSettingAsync(db, "AppName", TxtAppName.Text.Trim());
            await db.SaveChangesAsync();
            _audit.Log("Settings", "Update", "Organization information updated");
            ToastHelper.ShowSuccess("Organization settings saved.");
            if (Window.GetWindow(this) is MainWindow main)
                _ = main.RefreshOrgNameAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to save: {ex.Message}");
        }
    }

    private static async Task UpsertSettingAsync(MortuaryDbContext db, string key, string value)
    {
        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            db.Settings.Add(new Setting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.Now;
        }
    }

    private async void BtnSaveSmtp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new MortuaryDbContext();
            await UpsertSettingAsync(db, "SmtpHost", TxtSmtpHost.Text.Trim());
            await UpsertSettingAsync(db, "SmtpPort", TxtSmtpPort.Text.Trim());
            await UpsertSettingAsync(db, "SmtpUsername", TxtSmtpUsername.Text.Trim());
            await UpsertSettingAsync(db, "SmtpFromEmail", TxtSmtpFrom.Text.Trim());
            if (!string.IsNullOrWhiteSpace(TxtSmtpPassword.Password))
                await UpsertSettingAsync(db, "SmtpPassword", TxtSmtpPassword.Password);
            await db.SaveChangesAsync();
            ToastHelper.ShowSuccess("SMTP settings saved.");
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to save SMTP settings: {ex.Message}");
        }
    }

    private int? _selectedRoleId;

    private async void DgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgRoles.SelectedItem is not Role role) { PermPanel.IsEnabled = false; return; }

        _selectedRoleId = role.Id;
        TxtRoleName.Text = $"{role.Name} — {role.Description}";
        PermPanel.IsEnabled = true;
        BtnAddRole.IsEnabled = !role.IsSystem;

        var perms = JsonSerializer.Deserialize<HashSet<string>>(role.Permissions) ?? new HashSet<string>();
        foreach (var child in GetPermCheckboxes())
            child.IsChecked = perms.Contains(child.Tag?.ToString() ?? "");
    }

    private System.Collections.Generic.IEnumerable<CheckBox> GetPermCheckboxes()
    {
        foreach (var child in PermPanel.Children)
            if (child is CheckBox cb)
                yield return cb;
    }

    private async void BtnSavePerms_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRoleId == null) return;

        var selected = new HashSet<string>();
        foreach (var cb in GetPermCheckboxes())
            if (cb.IsChecked == true && cb.Tag is string tag)
                selected.Add(tag);

        try
        {
            using var db = new MortuaryDbContext();
            var role = await db.Roles.FindAsync(_selectedRoleId.Value);
            if (role == null) { ToastHelper.ShowError("Role not found."); return; }

            role.Permissions = JsonSerializer.Serialize(selected);
            role.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();

            _audit.Log("Settings", "Update Permissions", $"Updated permissions for role '{role.Name}'");
            ToastHelper.ShowSuccess($"Permissions saved for '{role.Name}'.");
            PermissionService.Invalidate();
            if (role.Name == App.CurrentUser?.Role)
                await PermissionService.LoadAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to save permissions: {ex.Message}");
        }
    }

    private async void BtnAddRole_Click(object sender, RoutedEventArgs e)
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox("Enter new role name:", "New Role", "");

        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim().ToLower().Replace(" ", "");

        try
        {
            using var db = new MortuaryDbContext();
            if (await db.Roles.AnyAsync(r => r.Name == name))
            {
                ToastHelper.ShowWarning($"Role '{name}' already exists.");
                return;
            }

            db.Roles.Add(new Role
            {
                Name = name,
                Description = "",
                Permissions = "[]",
                IsSystem = false
            });
            await db.SaveChangesAsync();
            _audit.Log("Settings", "Create Role", $"Created role '{name}'");
            ToastHelper.ShowSuccess($"Role '{name}' created.");
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to create role: {ex.Message}");
        }
    }

    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnCheckUpdate.IsEnabled = false;
        TxtUpdateStatus.Text = "Checking...";

        var info = await UpdateService.CheckForUpdateAsync();

        if (info == null)
        {
            TxtUpdateStatus.Text = $"v{UpdateService.CurrentVersion} — up to date";
            ToastHelper.ShowInfo("No updates available.");
        }
        else
        {
            var result = MessageBox.Show(
                $"Version {info.LatestVersion} is available!\n\n{info.ReleaseNotes}\n\nDownload and install now?",
                "Update Available",
                MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                TxtUpdateStatus.Text = "Downloading...";
                var zipPath = await UpdateService.DownloadUpdateAsync(info.DownloadUrl);

                if (zipPath != null)
                {
                    TxtUpdateStatus.Text = "Installing...";
                    UpdateService.InstallUpdate(zipPath);
                }
                else
                {
                    TxtUpdateStatus.Text = "Download failed";
                    ToastHelper.ShowError("Failed to download update.");
                }
            }
            else
            {
                TxtUpdateStatus.Text = $"v{info.LatestVersion} available";
            }
        }

        BtnCheckUpdate.IsEnabled = true;
    }

    private async void BtnSaveRevenuePct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new MortuaryDbContext();
            await UpsertSettingAsync(db, "RevenuePct", CboRevenuePct.SelectedValue?.ToString() ?? "100");
            await db.SaveChangesAsync();
            ToastHelper.ShowSuccess($"Revenue filter set to {CboRevenuePct.SelectedValue}%.");
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to save: {ex.Message}");
        }
    }
}
