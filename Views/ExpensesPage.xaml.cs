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

public partial class ExpensesPage : UserControl
{
    private readonly AuditService _audit = new();

    public ExpensesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgExpenses.ItemsSource = await db.Expenses.OrderByDescending(e => e.Date).ToListAsync();
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
            db.Expenses.Add(new Expense { Name = TxtName.Text.Trim(), Amount = amt, Date = DateTime.Now });
            await db.SaveChangesAsync();
            _audit.Log("Expenses", "Create", $"Expense recorded: {TxtName.Text} - {amt:N2}");
            await LoadDataAsync();
            TxtName.Clear(); TxtAmount.Text = "0";
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to add expense: {ex.Message}"); }
    }
}
