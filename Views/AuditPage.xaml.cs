using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;

namespace MortuaryApp.Views;

public partial class AuditPage : UserControl
{
    public AuditPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgAudit.ItemsSource = await db.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnFilter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var q = TxtFilterModule.Text.Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                await LoadAllAsync();
                return;
            }

            using var db = new MortuaryDbContext();
            DgAudit.ItemsSource = await db.AuditLogs.Where(a => a.Module.Contains(q))
                .OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to filter: {ex.Message}");
        }
    }

    private async void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TxtFilterModule.Clear();
        await LoadAllAsync();
    }
}
