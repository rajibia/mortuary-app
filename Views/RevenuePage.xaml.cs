using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;

namespace MortuaryApp.Views;

public partial class RevenuePage : UserControl
{
    public RevenuePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            var pctSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "RevenuePct");
            var pct = decimal.TryParse(pctSetting?.Value, out var v) ? v / 100m : 1m;

            var allIncomes = await db.Incomes.ToListAsync();

            var monthlyGroups = allIncomes
                .GroupBy(i => new { i.Date.Year, i.Date.Month })
                .Select(g => new
                {
                    Month = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                    SortKey = g.Key.Year * 100 + g.Key.Month,
                    Total = g.Sum(i => i.Amount) * pct
                })
                .OrderByDescending(x => x.SortKey)
                .ToList();

            DgMonthlyIncome.ItemsSource = monthlyGroups.Select(g => new
            {
                g.Month,
                g.Total
            }).ToList();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }
}
