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

public partial class IncomesPage : UserControl
{
    private readonly AuditService _audit = new();

    public IncomesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgIncomes.ItemsSource = await db.Incomes.OrderByDescending(i => i.Date).ToListAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text) || !decimal.TryParse(TxtAmount.Text, out var amt))
        {
            ToastHelper.ShowWarning("Enter name and valid amount.");
            return;
        }

        try
        {
            using var db = new MortuaryDbContext();
            db.Incomes.Add(new Income { Name = TxtName.Text.Trim(), Amount = amt, Date = DateTime.Now });
            await db.SaveChangesAsync();
            _audit.Log("Incomes", "Create", $"Income recorded: {TxtName.Text} - {amt:N2}");
            await LoadDataAsync();
            TxtName.Clear(); TxtAmount.Text = "0";
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to add income: {ex.Message}"); }
    }
}
