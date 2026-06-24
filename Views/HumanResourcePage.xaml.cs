using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class HumanResourcePage : UserControl
{
    private int? _editingUserId;

        public HumanResourcePage()
        {
            InitializeComponent();
            EmailHelper.WireValidation(TxtEmail);
            _ = LoadUsersAsync();
        }

    private async Task LoadUsersAsync()
    {
        using var db = new MortuaryDbContext();
        var users = await db.Users.OrderByDescending(u => u.Id).ToListAsync();
        DgUsers.ItemsSource = users;
        CboRole.ItemsSource = await db.Roles.OrderBy(r => r.Name).ToListAsync();

        var canManage = PermissionService.Has("actions.users.manage");
        BtnShowForm.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        FormCard.Visibility = canManage ? FormCard.Visibility : Visibility.Collapsed;
    }

    private void BtnShowForm_Click(object sender, RoutedEventArgs e)
    {
        _editingUserId = null;
        ClearForm();
        TxtFormTitle.Text = "New User";
        TxtPassword.IsEnabled = true;
        TxtConfirmPassword.IsEnabled = true;
        TxtPassword.Tag = null;
        FormCard.Visibility = Visibility.Visible;
        TxtFullName.Focus();
    }

    private void BtnCancelForm_Click(object sender, RoutedEventArgs e)
    {
        FormCard.Visibility = Visibility.Collapsed;
        ClearForm();
    }

    private void ClearForm()
    {
        TxtFullName.Text = "";
        TxtUsername.Text = "";
        TxtEmail.Text = "";
        TxtPassword.Password = "";
        TxtConfirmPassword.Password = "";
        CboRole.SelectedIndex = 1;
        ChkIsActive.IsChecked = true;
    }

    private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
    {
        var fullName = TxtFullName.Text.Trim();
        var username = TxtUsername.Text.Trim();
        var email = TxtEmail.Text.Trim();
        var password = TxtPassword.Password;
        var confirmPassword = TxtConfirmPassword.Password;
        var role = (CboRole.SelectedItem as Role)?.Name ?? "receptionist";
        var isActive = ChkIsActive.IsChecked ?? true;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
        {
            ToastHelper.ShowWarning("Full Name and Username are required.");
            return;
        }

        if (!string.IsNullOrEmpty(email) && !EmailHelper.IsValidEmail(email))
        {
            ToastHelper.ShowWarning("Enter a valid email address.");
            return;
        }

        if (_editingUserId == null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ToastHelper.ShowWarning("Password is required for new users.");
                return;
            }
            if (password != confirmPassword)
            {
                ToastHelper.ShowWarning("Passwords do not match.");
                return;
            }
            if (password.Length < 4)
            {
                ToastHelper.ShowWarning("Password must be at least 4 characters.");
                return;
            }
        }
        else if (!string.IsNullOrWhiteSpace(password))
        {
            if (password != confirmPassword)
            {
                ToastHelper.ShowWarning("Passwords do not match.");
                return;
            }
        }

        using var db = new MortuaryDbContext();

        if (_editingUserId == null)
        {
            var existing = await db.Users.AnyAsync(u => u.Username == username);
            if (existing)
            {
                ToastHelper.ShowWarning("Username already exists.");
                return;
            }

            var user = new User
            {
                FullName = fullName,
                Username = username,
                Email = email,
                PasswordHash = SecurityHelper.HashPassword(password),
                Role = role,
                IsActive = isActive,
                CanLogin = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            ToastHelper.ShowSuccess($"User '{username}' created successfully.");
        }
        else
        {
            var user = await db.Users.FindAsync(_editingUserId.Value);
            if (user == null)
            {
                ToastHelper.ShowError("User not found.");
                return;
            }

            user.FullName = fullName;
            user.Username = username;
            user.Email = email;
            user.Role = role;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(password))
                user.PasswordHash = SecurityHelper.HashPassword(password);

            await db.SaveChangesAsync();
            ToastHelper.ShowSuccess($"User '{username}' updated successfully.");

            if (user.Id == App.CurrentUser?.Id && Window.GetWindow(this) is MainWindow main)
                await main.RefreshCurrentUserAsync();
        }

        FormCard.Visibility = Visibility.Collapsed;
        ClearForm();
        await LoadUsersAsync();
    }

    private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int userId)
        {
            using var db = new MortuaryDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            _editingUserId = user.Id;
            TxtFormTitle.Text = "Edit User";
            TxtFullName.Text = user.FullName;
            TxtUsername.Text = user.Username;
            TxtEmail.Text = user.Email ?? "";
            TxtPassword.Password = "";
            TxtConfirmPassword.Password = "";
            TxtPassword.IsEnabled = true;
            TxtConfirmPassword.IsEnabled = true;
            ChkIsActive.IsChecked = user.IsActive;

            foreach (var item in CboRole.Items)
            {
                if (item is Role r && r.Name == user.Role)
                {
                    CboRole.SelectedItem = item;
                    break;
                }
            }

            FormCard.Visibility = Visibility.Visible;
            TxtFullName.Focus();
        }
    }

    private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int userId)
        {
            var tempPwd = SecurityHelper.GenerateRandomPassword(12);
            using var db = new MortuaryDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            user.PasswordHash = SecurityHelper.HashPassword(tempPwd);
            user.MustChangePassword = true;
            user.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();

            var msg = $"Temporary password for '{user.Username}':\n{tempPwd}\n\nUser must change on next login.";
            Clipboard.SetText(tempPwd);
            MessageBox.Show(msg, "Password Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            ToastHelper.ShowSuccess("Password reset. Copied to clipboard.");
        }
    }
}
