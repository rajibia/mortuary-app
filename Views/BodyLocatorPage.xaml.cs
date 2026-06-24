using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;

namespace MortuaryApp.Views;

public partial class BodyLocatorPage : UserControl
{
    public string? SearchQuery { get; set; }

    public BodyLocatorPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
        DgResults.SelectionChanged += DgResults_SelectionChanged;
    }

    public BodyLocatorPage(string? searchQuery) : this()
    {
        SearchQuery = searchQuery;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            TxtSearch.Text = SearchQuery;
        }
    }

    private async Task DoSearchAsync(string q)
    {
        try
        {
            q = q.ToLower();
            using var db = new MortuaryDbContext();
            DgResults.ItemsSource = await db.Bodies.Include(b => b.StorageLocation)
                .Where(b => b.MortuaryNumber.ToLower().Contains(q) ||
                            b.DeceasedName.ToLower().Contains(q) ||
                            (b.CauseOfDeath != null && b.CauseOfDeath.ToLower().Contains(q)))
                .OrderByDescending(b => b.CreatedAt).ToListAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to search: {ex.Message}"); }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgResults.ItemsSource = await db.Bodies.Include(b => b.StorageLocation)
                .OrderByDescending(b => b.CreatedAt).ToListAsync();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var q = TxtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(q)) { await LoadDataAsync(); return; }
        await DoSearchAsync(q);
    }

    private async void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Clear();
        await LoadDataAsync();
        ClearDetails();
    }

    private void ClearDetails()
    {
        DetailsCard.Visibility = Visibility.Collapsed;
        TblMortNo.Text = TblName.Text = TblStatus.Text = TblLocation.Text = TblKin.Text = TblCharges.Text = "";
    }

    private void DgResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DgResults.SelectedItem is not Models.MortuaryBody body)
        {
            ClearDetails();
            return;
        }

        DetailsCard.Visibility = Visibility.Visible;
        TblMortNo.Text = body.MortuaryNumber;
        TblName.Text = body.DeceasedName;
        TblStatus.Text = body.StatusDisplay;
        TblLocation.Text = body.StorageLocation?.DisplayName ?? "-";
        TblKin.Text = body.NextOfKinName ?? "-";
        TblCharges.Text = $"{body.BillingType} / \u20B5{body.BillingRate:N2}";
    }
}
