using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Services;

namespace MortuaryApp;

public partial class ForgotPasswordWindow : Window
{
    public ForgotPasswordWindow()
    {
        InitializeComponent();
        TxtEmail.Focus();
    }

    private async void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show("Please enter your email.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!EmailHelper.IsValidEmail(email))
        {
            MessageBox.Show("Enter a valid email address.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new MortuaryDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            MessageBox.Show("Email not found in the system.", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tempPassword = SecurityHelper.GenerateRandomPassword(12);
        user.PasswordHash = SecurityHelper.HashPassword(tempPassword);
        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        try
        {
            var orgName = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgName"))?.Value ?? "Mortuary System";
            await EmailService.SendAsync(email, "Password Reset - " + orgName,
                $"Your temporary password is: {tempPassword}\n\nYou will be required to change it on next login.\n\nThis is an automated message. Please do not reply.");

            TxtResult.Text = $"A temporary password has been sent to {email}. Please check your inbox.";
            TxtResult.Foreground = System.Windows.Media.Brushes.Green;
            TxtResult.Visibility = Visibility.Visible;
            BtnReset.IsEnabled = false;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "SMTP Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            TxtResult.Text = $"Password saved but failed to send email. Contact your administrator.\n\nTemporary password: {tempPassword}";
            TxtResult.Foreground = System.Windows.Media.Brushes.Red;
            TxtResult.Visibility = Visibility.Visible;
            BtnReset.IsEnabled = false;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
