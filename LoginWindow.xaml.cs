using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;

namespace MortuaryApp;

public partial class LoginWindow : Window
{
    public User? AuthenticatedUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        _ = LoadOrgNameAsync();
        TxtEmail.Focus();
        TxtEmail.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) TxtPassword.Focus();
        };
        TxtPassword.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) BtnLogin_Click(this, new RoutedEventArgs());
        };
    }

    private async Task LoadOrgNameAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var appName = await db.Settings.FirstOrDefaultAsync(s => s.Key == "AppName");
            TxtBrandName.Text = !string.IsNullOrWhiteSpace(appName?.Value) ? appName.Value : "Msoft Mortuary Pro";

            var logoPath = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgLogoPath"))?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            {
                ImgLoginLogo.Source = new BitmapImage(new Uri(Path.GetFullPath(logoPath)));
                ImgLoginLogo.Visibility = Visibility.Visible;
                LoginIconBox.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnClose_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TxtError.Text = "Please enter email and password.";
            return;
        }

        if (!EmailHelper.IsValidEmail(email))
        {
            TxtError.Text = "Enter a valid email address.";
            return;
        }

        BtnLogin.IsEnabled = false;
        BtnLogin.Content = "Signing in...";

        try
        {
            using var db = new MortuaryDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive && u.CanLogin);

            if (user == null)
            {
                TxtError.Text = "Invalid email or password.";
                BtnLogin.IsEnabled = true;
                return;
            }

            var valid = SecurityHelper.VerifyPassword(password, user.PasswordHash);
            if (!valid)
            {
                TxtError.Text = "Invalid email or password.";
                BtnLogin.IsEnabled = true;
                return;
            }

            AuthenticatedUser = user;
            DialogResult = true;
            Close();
        }
        catch
        {
            TxtError.Text = "Connection error. Please try again.";
            BtnLogin.IsEnabled = true;
        }
    }

    private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            TxtError.Text = "Enter your email first.";
            return;
        }

        var forgot = new ForgotPasswordWindow();
        forgot.ShowDialog();
    }
}
