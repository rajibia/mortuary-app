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

public partial class CremationsPage : UserControl
{
    private readonly AuditService _audit = new();

    public CremationsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            CboBody.ItemsSource = await db.Bodies.Where(b => b.Status == "released").ToListAsync();
            CboBody.DisplayMemberPath = "DeceasedName";
            DgCremations.ItemsSource = await db.Cremations.Include(c => c.Body)
                .OrderByDescending(c => c.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnSchedule_Click(object sender, RoutedEventArgs e)
    {
        if (CboBody.SelectedItem is not MortuaryBody body) return;
        using var db = new MortuaryDbContext();
        db.Cremations.Add(new Cremation
        {
            BodyId = body.Id,
            ScheduledAt = DateTime.Now,
            Status = "scheduled",
            AuthorizationDocument = TxtAuthDoc.Text.Trim()
        });
        await db.SaveChangesAsync();
        _audit.Log("Cremations", "Schedule", $"Cremation scheduled for {body.MortuaryNumber}");
        await LoadDataAsync();
    }

    private async void BtnComplete_Click(object sender, RoutedEventArgs e)
    {
        if (DgCremations.SelectedItem is not Cremation c) return;
        var dialog = new Window { Title = "Complete Cremation", Width = 400, Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
        var sp = new StackPanel { Margin = new Thickness(15) };
        sp.Children.Add(new TextBlock { Text = "Ashes Disposition:" });
        var txtAshes = new TextBox { Margin = new Thickness(0, 5, 0, 5), TextWrapping = TextWrapping.Wrap, Height = 60 };
        sp.Children.Add(txtAshes);
        sp.Children.Add(new TextBlock { Text = "Collected By:" });
        var txtCollectedBy = new TextBox { Margin = new Thickness(0, 5, 0, 15) };
        sp.Children.Add(txtCollectedBy);
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnSave = new Button { Content = "Complete", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var btnCancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        btnPanel.Children.Add(btnSave); btnPanel.Children.Add(btnCancel);
        sp.Children.Add(btnPanel);
        dialog.Content = sp;

        btnSave.Click += async (_, _) =>
        {
            using var db2 = new MortuaryDbContext();
            var crem = await db2.Cremations.FindAsync(c.Id);
            if (crem == null) return;
            crem.Status = "completed";
            crem.CrematedAt = DateTime.Now;
            crem.AshesDisposition = txtAshes.Text.Trim();
            crem.AshesCollectedBy = txtCollectedBy.Text.Trim();
            crem.AshesCollectedAt = DateTime.Now;
            await db2.SaveChangesAsync();
            _audit.Log("Cremations", "Complete", $"Cremation #{c.Id} completed");
            dialog.Close();
            await LoadDataAsync();
        };
        btnCancel.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();
}
