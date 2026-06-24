using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class EmbalmingPage : UserControl
{
    private readonly AuditService _audit = new();
    private readonly TimelineService _timeline = new();

    public EmbalmingPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            var active = await db.EmbalmingRecords.Include(r => r.Body)
                .Where(r => r.Status == "in_progress" || r.Status == "pending").ToListAsync();

            DgEmbalming.ItemsSource = active.OrderByDescending(r => r.CreatedAt).ToList();
            DgOldEmbalming.ItemsSource = await db.EmbalmingRecords.Include(r => r.Body)
                .Where(r => r.Status == "completed").OrderByDescending(r => r.CompletedAt).ToListAsync();

        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private async void BtnNewEmbalming_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new MortuaryDbContext();
            var bodies = await db.Bodies.Where(b => b.Status == "admitted" || b.Status == "identified" || b.Status == "stored").ToListAsync();
            if (bodies.Count == 0) { ToastHelper.ShowInfo("No eligible bodies."); return; }

            var dialog = new Window { Title = "New Embalming Record", Width = 380, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
            var sp = new StackPanel { Margin = new Thickness(15) };
            sp.Children.Add(new TextBlock { Text = "Select Body:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbo = new ComboBox { Margin = new Thickness(0, 0, 0, 15), DisplayMemberPath = "DeceasedName", ItemsSource = bodies };
            sp.Children.Add(cbo);
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "  Start", Style = FindResource("PrimaryBtn") as Style, Width = 90, IsDefault = true };
            var btnCancel = new Button { Content = "Cancel", Width = 80, IsCancel = true, Margin = new Thickness(10, 0, 0, 0) };
            btnPanel.Children.Add(btnSave); btnPanel.Children.Add(btnCancel);
            sp.Children.Add(btnPanel);
            dialog.Content = sp;

            btnSave.Click += async (_, _) =>
            {
                try
                {
                    if (cbo.SelectedItem is not MortuaryBody body) return;
                    using var db2 = new MortuaryDbContext();
                    db2.EmbalmingRecords.Add(new EmbalmingRecord { BodyId = body.Id, Status = "in_progress", StartedAt = DateTime.Now });
                    await db2.SaveChangesAsync();
                    _timeline.AddEvent(body.Id, "embalming_started", "Embalming started");
                    _audit.Log("Embalming", "Create", $"Started embalming for {body.MortuaryNumber}");
                    dialog.Close(); await LoadDataAsync();
                }
                catch (Exception ex) { ToastHelper.ShowError($"Failed to create embalming record: {ex.Message}"); }
            };
            btnCancel.Click += (_, _) => dialog.Close();
            dialog.ShowDialog();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to open new embalming dialog: {ex.Message}"); }
    }

    private async void BtnStartProcedure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DgEmbalming.SelectedItem is not EmbalmingRecord rec) return;
            using var db = new MortuaryDbContext();
            var r = await db.EmbalmingRecords.FindAsync(rec.Id);
            if (r == null) return;
            if (r.Status == "in_progress") { ToastHelper.ShowInfo("Already in progress."); return; }
            r.Status = "in_progress";
            r.StartedAt ??= DateTime.Now;
            await db.SaveChangesAsync();
            _audit.Log("Embalming", "Start", $"Procedure started for record #{rec.Id}");
            await LoadDataAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to start procedure: {ex.Message}"); }
    }

    private async void BtnComplete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DgEmbalming.SelectedItem is not EmbalmingRecord rec) return;
            var result = MessageBox.Show("Mark this embalming as complete?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            using var db = new MortuaryDbContext();
            var r = await db.EmbalmingRecords.Include(e => e.Body).FirstAsync(e => e.Id == rec.Id);
            r.Status = "completed"; r.CompletedAt = DateTime.Now;
            if (r.Body != null)
            {
                r.Body.Status = "embalmed";
                r.Body.UpdatedAt = DateTime.Now;
            }
            await db.SaveChangesAsync();
            _timeline.AddEvent(r.BodyId, "embalming_completed", "Embalming completed");
            _audit.Log("Embalming", "Complete", $"Embalming #{rec.Id} completed");
            await LoadDataAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to complete embalming: {ex.Message}"); }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        try { await LoadDataAsync(); }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to refresh: {ex.Message}"); }
    }
}
