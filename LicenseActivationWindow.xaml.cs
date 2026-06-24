using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Services;

namespace MortuaryApp;

public partial class LicenseActivationWindow : Window
{
    public LicenseActivationWindow()
    {
        InitializeComponent();
        TxtMachineId.Text = LicenseService.GetMachineFingerprint();
    }

    private void TxtMachineId_GotFocus(object sender, RoutedEventArgs e) => TxtMachineId.SelectAll();

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private async void BtnActivate_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            TxtError.Text = "Please enter a license key.";
            return;
        }

        if (LicenseService.ValidateLicense(key))
        {
            try
            {
                using var db = new MortuaryDbContext();
                var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "license_key");
                if (setting != null)
                {
                    setting.Value = key;
                    setting.UpdatedAt = System.DateTime.Now;
                }
                else
                {
                    db.Settings.Add(new Models.Setting { Key = "license_key", Value = key });
                }
                await db.SaveChangesAsync();
            }
            catch { }

            DialogResult = true;
            Close();
        }
        else
        {
            TxtError.Text = "Invalid license key. Please check and try again.";
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
