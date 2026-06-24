using System.Windows;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;

namespace MortuaryApp;

public partial class ChangePasswordWindow : Window
{
    private readonly User _user;

    public ChangePasswordWindow(User user)
    {
        InitializeComponent();
        _user = user;
        TxtNewPassword.Focus();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var newPass = TxtNewPassword.Password;
        var confirm = TxtConfirmPassword.Password;

        if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
        {
            MessageBox.Show("Password must be at least 6 characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (newPass != confirm)
        {
            MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new MortuaryDbContext();
        var user = db.Users.Find(_user.Id);
        if (user != null)
        {
            user.PasswordHash = SecurityHelper.HashPassword(newPass);
            user.MustChangePassword = false;
            db.SaveChanges();
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
