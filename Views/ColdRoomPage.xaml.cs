using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class ColdRoomPage : UserControl
{
    private readonly AuditService _audit = new();

    public ColdRoomPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgLocations.ItemsSource = await db.StorageLocations.Include(s => s.Bodies)
                .OrderBy(s => s.Room).ThenBy(s => s.Chamber).ThenBy(s => s.Bed).ToListAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private static string ToTitle(string input) =>
        string.IsNullOrWhiteSpace(input) ? string.Empty
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.Trim().ToLower());

    private async void BtnAddLocation_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRoom.Text) || string.IsNullOrWhiteSpace(TxtBed.Text))
        {
            ToastHelper.ShowWarning("Room and Bed are required.");
            return;
        }

        try
        {
            var room = ToTitle(TxtRoom.Text);
            var chamber = ToTitle(TxtChamber.Text);
            var bed = ToTitle(TxtBed.Text);

            using var db = new MortuaryDbContext();
            var exists = await db.StorageLocations.AnyAsync(l =>
                l.Room == room &&
                (l.Chamber ?? "") == chamber &&
                l.Bed == bed);

            if (exists)
            {
                ToastHelper.ShowWarning("This location already exists.");
                return;
            }

            db.StorageLocations.Add(new StorageLocation
            {
                Room = room,
                Chamber = string.IsNullOrWhiteSpace(chamber) ? null : chamber,
                Bed = bed,
                Status = "available"
            });
            await db.SaveChangesAsync();
            _audit.Log("ColdRoom", "Create", $"Added bed {room}/{bed}");
            await LoadDataAsync();
            TxtRoom.Clear(); TxtChamber.Clear(); TxtBed.Clear();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to add location: {ex.Message}"); }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private void BtnEditStatus_Click(object sender, RoutedEventArgs e)
    {
        if (DgLocations.SelectedItem is not StorageLocation loc) return;

        var dialog = new Window
        {
            Title = "Change Status",
            Width = 320,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            BorderThickness = new Thickness(0),
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true
        };
        var outerBorder = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0)),
            BorderThickness = new Thickness(1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 20,
                Opacity = 0.15,
                Color = System.Windows.Media.Colors.Black
            }
        };
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(new TextBlock
        {
            Text = $"Location: {loc.DisplayName}",
            Margin = new Thickness(0, 0, 0, 16),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.Black
        });
        var cbo = new ComboBox
        {
            Margin = new Thickness(0, 0, 0, 20),
            Height = 36,
            FontSize = 13
        };
        cbo.Items.Add("available"); cbo.Items.Add("occupied"); cbo.Items.Add("maintenance"); cbo.Items.Add("cleaning");
        cbo.Text = loc.Status;
        sp.Children.Add(cbo);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnSave = new Button
        {
            Content = "Save",
            Width = 80,
            Height = 34,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true,
            Cursor = System.Windows.Input.Cursors.Hand,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };
        var btnCancel = new Button
        {
            Content = "Cancel",
            Width = 80,
            Height = 34,
            IsCancel = true,
            Cursor = System.Windows.Input.Cursors.Hand,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Background = System.Windows.Media.Brushes.White,
            Foreground = System.Windows.Media.Brushes.Black,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD1, 0xD5, 0xDB)),
            BorderThickness = new Thickness(1)
        };
        btnPanel.Children.Add(btnSave); btnPanel.Children.Add(btnCancel);
        sp.Children.Add(btnPanel);
        outerBorder.Child = sp;
        dialog.Content = outerBorder;

        btnSave.Click += async (_, _) =>
        {
            try
            {
                using var db2 = new MortuaryDbContext();
                var l = await db2.StorageLocations.FindAsync(loc.Id);
                if (l != null)
                {
                    l.Status = cbo.Text;
                    await db2.SaveChangesAsync();
                }
                _audit.Log("ColdRoom", "Update", $"Changed {loc.DisplayName} status to {cbo.Text}");
                dialog.Close();
                await LoadDataAsync();
            }
            catch (Exception ex) { ToastHelper.ShowError($"Failed to update status: {ex.Message}"); }
        };
        btnCancel.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }

    private async void BtnDeleteLocation_Click(object sender, RoutedEventArgs e)
    {
        if (DgLocations.SelectedItem is not StorageLocation loc) return;
        await ConfirmDeleteLocation(loc);
    }

    private async Task ConfirmDeleteLocation(StorageLocation loc)
    {
        var result = MessageBox.Show($"Delete {loc.DisplayName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = new MortuaryDbContext();
            var entity = await db.StorageLocations.FindAsync(loc.Id);
            if (entity != null) db.StorageLocations.Remove(entity);
            await db.SaveChangesAsync();
            _audit.Log("ColdRoom", "Delete", $"Deleted {loc.DisplayName}");
            await LoadDataAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to delete location: {ex.Message}"); }
    }
}
