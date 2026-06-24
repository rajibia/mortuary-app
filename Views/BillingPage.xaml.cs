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

public partial class BillingPage : UserControl
{
    private readonly AuditService _audit = new();

    public BillingPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var bodies = await db.Bodies.ToListAsync();
            CboBody.ItemsSource = bodies;
            CboBody.DisplayMemberPath = "DeceasedName";
            CboChargeType.ItemsSource = await db.ChargeTypes.Where(c => c.IsActive).ToListAsync();
            CboChargeType.DisplayMemberPath = "Name";
            DgCharges.ItemsSource = await db.Charges.Include(c => c.Body).OrderByDescending(c => c.CreatedAt).ToListAsync();
            DgChargeTypes.ItemsSource = await db.ChargeTypes.ToListAsync();
            var allCharges = await db.Charges.ToListAsync();

            DgBillableBodies.ItemsSource = bodies
                .Where(b => b.Status == "admitted" && b.AmountToBePaid > 0)
                .Select(b => new BillableBodyViewModel
                {
                    Id = b.Id,
                    MortuaryNumber = b.MortuaryNumber,
                    DeceasedName = b.DeceasedName,
                    AmountToBePaid = b.AmountToBePaid,
                    BillingRate = b.BillingRate,
                    DepositAmount = b.DepositAmount,
                    BillingType = b.BillingType,
                    BillingStartAt = b.BillingStartAt,
                    CurrentBalance = ComputeBalance(b),
                    StoragePaid = allCharges
                        .Where(c => c.BodyId == b.Id && c.Description.StartsWith("Billing payment"))
                        .Sum(c => c.PaidAmount)
                }).ToList();

            var releases = await db.ReleaseRecords.Include(r => r.Body).ToListAsync();
            DgPaidBodies.ItemsSource = releases
                .Where(r => r.Body != null && r.Body.Status == "released")
                .Select(r => new PaidBodyViewModel
                {
                    MortuaryNumber = r.Body!.MortuaryNumber,
                    DeceasedName = r.Body!.DeceasedName,
                    AmountToBePaid = r.Body!.AmountToBePaid,
                    DepositAmount = r.Body!.DepositAmount,
                    TotalPaid = allCharges.Where(c => c.BodyId == r.BodyId).Sum(c => c.PaidAmount),
                    ReleasedTo = r.ReleasedTo,
                    ReleasedAt = r.ReleasedAt,
                    Status = r.Status
                }).ToList();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private async void BtnAddCharge_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CboBody.SelectedItem is not MortuaryBody body || string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                ToastHelper.ShowWarning("Select body and enter description.");
                return;
            }

            using var db = new MortuaryDbContext();
            db.Charges.Add(new Charge
            {
                BodyId = body.Id,
                ChargeTypeId = (CboChargeType.SelectedItem as ChargeType)?.Id,
                Description = TxtDescription.Text.Trim(),
                Amount = decimal.TryParse(TxtAmount.Text, out var amt) ? amt : 0,
                Status = "pending"
            });
            await db.SaveChangesAsync();
            _audit.Log("Billing", "Add Charge", $"Added charge to {body.MortuaryNumber}");
            await LoadDataAsync();
            TxtDescription.Clear(); TxtAmount.Text = "0"; TxtNotes.Clear();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to add charge: {ex.Message}"); }
    }

    private async void BtnRecordPayment_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionService.Has("actions.billing.pay"))
        {
            ToastHelper.ShowWarning("You do not have permission to process payments.");
            return;
        }
        try
        {
            if (DgCharges.SelectedItem is not Charge charge) return;

            var dialog = new Window { Title = "Record Payment", Width = 350, Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
            var sp = new StackPanel { Margin = new Thickness(15) };
            sp.Children.Add(new TextBlock { Text = $"Charge: {charge.Description} ({charge.Amount:N2})" });
            sp.Children.Add(new TextBlock { Text = $"Balance: {charge.Balance:N2}" });
            sp.Children.Add(new TextBlock { Text = "Payment Amount:" });
            var txtAmount = new TextBox { Text = charge.Balance.ToString("N2"), Margin = new Thickness(0, 5, 0, 15) };
            sp.Children.Add(txtAmount);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnPay = new Button { Content = "Pay", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            var btnCancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };
            btnPanel.Children.Add(btnPay); btnPanel.Children.Add(btnCancel);
            sp.Children.Add(btnPanel);
            dialog.Content = sp;

            btnPay.Click += async (_, _) =>
            {
                try
                {
                    if (!decimal.TryParse(txtAmount.Text, out var paid) || paid <= 0) return;
                    using var db2 = new MortuaryDbContext();
                    var ch = await db2.Charges.FindAsync(charge.Id);
                    if (ch == null) { ToastHelper.ShowError("Charge no longer exists."); return; }
                    var maxPayment = Math.Max(0, ch.Amount - ch.PaidAmount);
                    ch.PaidAmount += Math.Min(paid, maxPayment);
                    ch.PaidAt = DateTime.Now;
                    ch.Status = ch.PaidAmount >= ch.Amount ? "paid" : "partial";
                    db2.Incomes.Add(new Income
                    {
                        IncomeHead = 0,
                        Name = $"Payment - Charge #{charge.Id}",
                        Date = DateTime.Now,
                        Amount = Math.Min(paid, maxPayment),
                        Description = $"Charge: {charge.Description}, Amount: {Math.Min(paid, maxPayment):N2}",
                        CreatedBy = App.CurrentUser?.Id
                    });
                    await db2.SaveChangesAsync();
                    _audit.Log("Billing", "Payment", $"Payment of {paid:N2} recorded for charge #{charge.Id}");
                    ToastHelper.ShowSuccess("Payment recorded successfully!");
                    dialog.Close();
                    await LoadDataAsync();
                    var receipt = new ReceiptWindow(charge.Id);
                    receipt.Owner = Window.GetWindow(this);
                    receipt.ShowDialog();
                }
                catch (Exception ex) { ToastHelper.ShowError($"Failed to record payment: {ex.Message}"); }
            };
            btnCancel.Click += (_, _) => dialog.Close();
            dialog.ShowDialog();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to open payment dialog: {ex.Message}"); }
    }

    private void DgBillableBodies_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgBillableBodies.SelectedItem is BillableBodyViewModel vm)
        {
            TxtSelectedName.Text = $"{vm.MortuaryNumber} - {vm.DeceasedName}";
            TxtSelectedBalance.Text = $"Balance: \u20B5{vm.NetBalance:N2}";
            TxtPayAmount.Text = vm.NetBalance.ToString("N2");
            PaymentPanel.Visibility = Visibility.Visible;
        }
        else
        {
            PaymentPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnPaySelected_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionService.Has("actions.billing.pay"))
        {
            ToastHelper.ShowWarning("You do not have permission to process payments.");
            return;
        }
        if (DgBillableBodies.SelectedItem is not BillableBodyViewModel vm) return;
        if (!decimal.TryParse(TxtPayAmount.Text, out var paid) || paid <= 0)
        {
            ToastHelper.ShowWarning("Enter a valid payment amount.");
            return;
        }

        var result = MessageBox.Show("Do you wish to proceed to make payment?", "Confirm Payment",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = new MortuaryDbContext();
            var body = await db.Bodies.FindAsync(vm.Id);
            if (body == null) { ToastHelper.ShowError("Body no longer exists."); return; }

            var actualBalance = vm.NetBalance;
            var appliedAmount = Math.Min(paid, actualBalance);
            var change = Math.Max(0, paid - actualBalance);

            var charge = new Charge
            {
                BodyId = body.Id,
                Description = $"Billing payment - {body.MortuaryNumber}",
                Amount = actualBalance,
                PaidAmount = appliedAmount,
                PaidAt = DateTime.Now,
                Status = paid >= actualBalance ? "paid" : "partial"
            };
            db.Charges.Add(charge);
            db.Incomes.Add(new Income
            {
                IncomeHead = 0,
                Name = $"Payment - {body.MortuaryNumber}",
                Date = DateTime.Now,
                Amount = appliedAmount,
                Description = $"Body: {body.DeceasedName}, Payment: {appliedAmount:N2}, Change: {change:N2}",
                CreatedBy = App.CurrentUser?.Id
            });
            await db.SaveChangesAsync();

            _audit.Log("Billing", "Payment", $"Payment of {paid:N2} (change {change:N2}) recorded for {body.MortuaryNumber}");
            ToastHelper.ShowSuccess("Payment recorded successfully!");

            PaymentPanel.Visibility = Visibility.Collapsed;
            await LoadDataAsync();

            var receipt = new ReceiptWindow(charge.Id, paid, change);
            receipt.Owner = Window.GetWindow(this);
            receipt.ShowDialog();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Payment failed: {ex.Message}"); }
    }

    private async void BtnAddChargeType_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Window { Title = "New Charge Type", Width = 350, Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this) };
            var sp = new StackPanel { Margin = new Thickness(15) };
            sp.Children.Add(new TextBlock { Text = "Name:" });
            var txtName = new TextBox { Margin = new Thickness(0, 5, 0, 5) };
            sp.Children.Add(txtName);
            sp.Children.Add(new TextBlock { Text = "Code:" });
            var txtCode = new TextBox { Margin = new Thickness(0, 5, 0, 5) };
            sp.Children.Add(txtCode);
            sp.Children.Add(new TextBlock { Text = "Default Amount:" });
            var txtAmt = new TextBox { Text = "0", Margin = new Thickness(0, 5, 0, 15) };
            sp.Children.Add(txtAmt);

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
                    if (string.IsNullOrWhiteSpace(txtName.Text)) return;
                    using var db2 = new MortuaryDbContext();
                    db2.ChargeTypes.Add(new ChargeType
                    {
                        Name = txtName.Text.Trim(),
                        Code = txtCode.Text.Trim().ToUpper().Replace(" ", "_"),
                        DefaultAmount = decimal.TryParse(txtAmt.Text, out var d) ? d : 0
                    });
                    await db2.SaveChangesAsync();
                    dialog.Close();
                    await LoadDataAsync();
                }
                catch (Exception ex) { ToastHelper.ShowError($"Failed to add charge type: {ex.Message}"); }
            };
            btnCancel.Click += (_, _) => dialog.Close();
            dialog.ShowDialog();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to open charge type dialog: {ex.Message}"); }
    }

    private static decimal ComputeBalance(MortuaryBody body)
    {
        if (body.BillingStartAt == null || body.BillingRate <= 0) return body.AmountToBePaid - body.DepositAmount;

        var elapsed = DateTime.Now - body.BillingStartAt.Value;
        if (elapsed.TotalSeconds < 0) return body.AmountToBePaid - body.DepositAmount;

        double cycleHours = body.BillingType switch
        {
            "daily" => 24,
            "weekly" => 168,
            "monthly" => 730,
            _ => 0
        };

        if (cycleHours <= 0) return body.AmountToBePaid - body.DepositAmount;
        var cycles = (int)Math.Floor(elapsed.TotalHours / cycleHours) + 1;
        return body.AmountToBePaid - body.DepositAmount + (body.BillingRate * cycles);
    }
}

public class BillableBodyViewModel
{
    public int Id { get; set; }
    public string MortuaryNumber { get; set; } = "";
    public string DeceasedName { get; set; } = "";
    public decimal AmountToBePaid { get; set; }
    public decimal BillingRate { get; set; }
    public decimal DepositAmount { get; set; }
    public string BillingType { get; set; } = "";
    public DateTime? BillingStartAt { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal StoragePaid { get; set; }
    public decimal NetBalance => Math.Max(0, CurrentBalance - StoragePaid);
    public string PaymentStatus => NetBalance <= 0 ? "Paid" : "Pending";
}

public class PaidBodyViewModel
{
    public string MortuaryNumber { get; set; } = "";
    public string DeceasedName { get; set; } = "";
    public decimal AmountToBePaid { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public string ReleasedTo { get; set; } = "";
    public DateTime? ReleasedAt { get; set; }
    public string Status { get; set; } = "";
}
