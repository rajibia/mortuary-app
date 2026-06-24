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

public partial class InventoryPage : UserControl
{
    private readonly AuditService _audit = new();

    public InventoryPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            DgInventory.ItemsSource = await db.InventoryItems.OrderBy(i => i.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnAddItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ToastHelper.ShowWarning("Name is required.");
                return;
            }

            using var db = new MortuaryDbContext();
            db.InventoryItems.Add(new InventoryItem
            {
                Name = TxtName.Text.Trim(),
                Sku = TxtSku.Text.Trim(),
                Category = CboCategory.Text,
                Quantity = int.TryParse(TxtQty.Text, out var q) ? q : 0,
                Unit = CboUnit.Text
            });
            await db.SaveChangesAsync();
            _audit.Log("Inventory", "Create", $"Added item {TxtName.Text}");
            await LoadDataAsync();
            TxtName.Clear(); TxtSku.Clear(); TxtQty.Text = "0";
            ToastHelper.ShowSuccess("Item added successfully.");
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to add item: {ex.Message}");
        }
    }

    private void BtnAdjust_Click(object sender, RoutedEventArgs e)
    {
        if (DgInventory.SelectedItem is not InventoryItem item) return;

        var dialog = new Window { Title = "Adjust Stock", Width = 350, Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
        var sp = new StackPanel { Margin = new Thickness(15) };
        sp.Children.Add(new TextBlock { Text = $"Item: {item.Name} (Current: {item.Quantity})" });
        sp.Children.Add(new TextBlock { Text = "Type:" });
        var cboType = new ComboBox { Margin = new Thickness(0, 5, 0, 5) };
        cboType.Items.Add("in"); cboType.Items.Add("out"); cboType.SelectedIndex = 0;
        sp.Children.Add(cboType);
        sp.Children.Add(new TextBlock { Text = "Quantity:" });
        var txtQty = new TextBox { Margin = new Thickness(0, 5, 0, 15) };
        sp.Children.Add(txtQty);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnSave = new Button { Content = "Save", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var btnCancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        btnPanel.Children.Add(btnSave); btnPanel.Children.Add(btnCancel);
        sp.Children.Add(btnPanel);
        dialog.Content = sp;

        btnSave.Click += async (_, _) =>
        {
            try
            {
                if (!int.TryParse(txtQty.Text, out var qty) || qty <= 0) return;
                using var db2 = new MortuaryDbContext();
                var inv = await db2.InventoryItems.FindAsync(item.Id);
                if (inv == null) return;
                if (cboType.Text == "in") inv.Quantity += qty;
                else inv.Quantity = System.Math.Max(0, inv.Quantity - qty);
                db2.InventoryTransactions.Add(new InventoryTransaction
                {
                    ItemId = item.Id,
                    Type = cboType.Text,
                    Quantity = qty
                });
                await db2.SaveChangesAsync();
                _audit.Log("Inventory", "Adjust", $"Stock {cboType.Text} {qty} for {item.Name}");
                dialog.Close();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                ToastHelper.ShowError($"Failed to adjust stock: {ex.Message}");
            }
        };
        btnCancel.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }
}
