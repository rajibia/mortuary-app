using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Models;

namespace MortuaryApp.Views;

public partial class TemperaturePage : UserControl
{
    public TemperaturePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            CboLocation.ItemsSource = await db.StorageLocations.ToListAsync();
            CboLocation.DisplayMemberPath = "DisplayName";
            DgTemps.ItemsSource = await db.TemperatureLogs.Include(t => t.StorageLocation)
                .OrderByDescending(t => t.RecordedAt).Take(50).ToListAsync();
            DgAlerts.ItemsSource = await db.TemperatureLogs.Include(t => t.StorageLocation)
                .Where(t => t.Temperature < 2 || t.Temperature > 10)
                .OrderByDescending(t => t.RecordedAt).Take(20).ToListAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private async void BtnLog_Click(object sender, RoutedEventArgs e)
    {
        if (CboLocation.SelectedItem is not StorageLocation loc || !decimal.TryParse(TxtTemp.Text, out var temp))
        {
            ToastHelper.ShowWarning("Select location and enter a valid temperature.");
            return;
        }

        try
        {
            using var db = new MortuaryDbContext();
            db.TemperatureLogs.Add(new TemperatureLog
            {
                StorageLocationId = loc.Id,
                Temperature = temp,
                RecordedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            if (temp < 2 || temp > 10)
                ToastHelper.ShowWarning($"ALERT: Temperature {temp}°C at {loc.DisplayName} is outside safe range (2-10°C)!");

            await LoadDataAsync();
            TxtTemp.Clear();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to log temperature: {ex.Message}"); }
    }
}
