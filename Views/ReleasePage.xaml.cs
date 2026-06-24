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
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class ReleasePage : UserControl
{
    private readonly AuditService _audit = new();
    private readonly TimelineService _timeline = new();

    public ReleasePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            CboBody.ItemsSource = await db.Bodies.Where(b => b.Status != "released" && b.DeletedAt == null).ToListAsync();
            CboBody.DisplayMemberPath = "DeceasedName";
            DgPending.ItemsSource = await db.ReleaseRecords.Include(r => r.Body)
                .Where(r => r.Status == "pending").OrderByDescending(r => r.CreatedAt).ToListAsync();
            DgApproved.ItemsSource = await db.ReleaseRecords.Include(r => r.Body)
                .Where(r => r.Status == "approved" || r.Status == "completed").OrderByDescending(r => r.ReleasedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void CboBody_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (CboBody.SelectedItem is not MortuaryBody body)
            {
                BillsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            using var db = new MortuaryDbContext();
            var charges = await db.Charges.Where(c => c.BodyId == body.Id)
                .OrderByDescending(c => c.CreatedAt).ToListAsync();

            DgBills.ItemsSource = charges;

            var manualCharges = charges.Where(c => !c.Description.StartsWith("Billing payment")).ToList();
            var storageCharges = charges.Where(c => c.Description.StartsWith("Billing payment")).ToList();
            var manualAmount = manualCharges.Sum(c => c.Amount);
            var manualPaid = manualCharges.Sum(c => c.PaidAmount);
            var manualBalance = manualAmount - manualPaid;
            var storagePaid = storageCharges.Sum(c => c.PaidAmount);
            var storageBalance = Math.Max(0, ComputeBalance(body) - storagePaid);

            BillsSummary.Children.Clear();
            if (manualCharges.Count > 0)
            {
                AddSummaryItem(BillsSummary, "Manual Charges:", manualAmount);
                AddSummaryItem(BillsSummary, "Manual Paid:", manualPaid);
                AddSummaryItem(BillsSummary, "Manual Balance:", manualBalance);
            }
            if (storageBalance != 0 || storagePaid > 0)
            {
                AddSummaryItem(BillsSummary, "Storage Accrued:", ComputeBalance(body));
                AddSummaryItem(BillsSummary, "Storage Paid:", storagePaid);
                AddSummaryItem(BillsSummary, "Storage Balance:", storageBalance);
            }

            var totalOutstanding = manualBalance + storageBalance;

            if (totalOutstanding > 0)
            {
                TxtBillsStatus.Text = $"\u274C Cannot release — Total outstanding of \u20B5{totalOutstanding:N2}. Clear all bills first.";
                TxtBillsStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                BtnInitiate.IsEnabled = false;
            }
            else
            {
                TxtBillsStatus.Text = "\u2705 All bills cleared. Release can proceed.";
                TxtBillsStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                BtnInitiate.IsEnabled = true;
            }

            BillsPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load bills: {ex.Message}");
        }
    }

    private static decimal ComputeBalance(MortuaryBody body)
    {
        if (body.BillingStartAt == null || body.BillingRate <= 0) return body.AmountToBePaid - body.DepositAmount;
        var elapsed = DateTime.Now - body.BillingStartAt.Value;
        if (elapsed.TotalSeconds < 0) return body.AmountToBePaid - body.DepositAmount;
        double cycleHours = body.BillingType switch
        {
            "daily" => 24, "weekly" => 168, "monthly" => 730, _ => 0
        };
        if (cycleHours <= 0) return body.AmountToBePaid - body.DepositAmount;
        var cycles = (int)Math.Floor(elapsed.TotalHours / cycleHours) + 1;
        return body.AmountToBePaid - body.DepositAmount + (body.BillingRate * cycles);
    }

    private static void AddSummaryItem(Panel parent, string label, decimal amount)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 20, 0) };
        sp.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)), Margin = new Thickness(0, 0, 6, 0) });
        sp.Children.Add(new TextBlock { Text = $"\u20B5{amount:N2}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)) });
        parent.Children.Add(sp);
    }

    private async void BtnInitiate_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionService.Has("actions.release.approve"))
        {
            ToastHelper.ShowWarning("You do not have permission to approve releases.");
            return;
        }
        try
        {
            if (CboBody.SelectedItem is not MortuaryBody body || string.IsNullOrWhiteSpace(TxtReleasedTo.Text))
            {
                ToastHelper.ShowWarning("Select a body and enter recipient name.");
                return;
            }

            var phone = TxtPhone.FullNumber;

            using var balanceDb = new MortuaryDbContext();
            var attached = await balanceDb.Bodies.FindAsync(body.Id);
            if (attached == null) return;
            var allCharges = await balanceDb.Charges.Where(c => c.BodyId == body.Id).ToListAsync();
            var manualUnpaid = allCharges.Where(c => !c.Description.StartsWith("Billing payment")).Sum(c => c.Amount - c.PaidAmount);
            var storagePaid = allCharges.Where(c => c.Description.StartsWith("Billing payment")).Sum(c => c.PaidAmount);
            var storageUnpaid = Math.Max(0, ComputeBalance(attached) - storagePaid);
            var totalUnpaid = manualUnpaid + storageUnpaid;
            if (totalUnpaid > 0)
            {
                ToastHelper.ShowWarning($"Cannot release: total outstanding of \u20B5{totalUnpaid:N2}. Clear all bills first.");
                return;
            }

            using var db = new MortuaryDbContext();
            db.ReleaseRecords.Add(new ReleaseRecord
            {
                BodyId = body.Id,
                ReleasedTo = TxtReleasedTo.Text.Trim(),
                Relationship = TxtRelationship.Text.Trim(),
                IdNumber = TxtIdNumber.Text.Trim(),
                Phone = phone,
                Status = "pending",
                ChecklistNotes = TxtNotes.Text.Trim()
            });
            var bodyEntity = await db.Bodies.FindAsync(body.Id);
            if (bodyEntity != null)
            {
                bodyEntity.Status = "ready_for_release";
                bodyEntity.UpdatedAt = DateTime.Now;
            }
            await db.SaveChangesAsync();
            _timeline.AddEvent(body.Id, "release_initiated", $"Release initiated to {TxtReleasedTo.Text}");
            _audit.Log("Release", "Initiate", $"Release initiated for {body.MortuaryNumber}");
            await LoadDataAsync();
            ToastHelper.ShowSuccess("Release initiated. Requires approval.");
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to initiate release: {ex.Message}");
        }
    }

    private async void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DgPending.SelectedItem is not ReleaseRecord rec) return;
            var result = MessageBox.Show("Approve this release?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            using var db = new MortuaryDbContext();
            var r = await db.ReleaseRecords.Include(rel => rel.Body).FirstAsync(rel => rel.Id == rec.Id);
            r.Status = "completed";
            r.ReleasedAt = DateTime.Now;

            if (r.Body != null)
            {
                // Free the cold room location
                if (r.Body.StorageLocationId.HasValue)
                {
                    var loc = await db.StorageLocations.FindAsync(r.Body.StorageLocationId.Value);
                    if (loc != null)
                    {
                        loc.Status = "available";
                        loc.UpdatedAt = DateTime.Now;
                    }
                }
                r.Body.Status = "released";
                r.Body.StorageLocationId = null;
                _timeline.AddEvent(r.Body.Id, "released", $"Body released to {r.ReleasedTo}");
            }

            await db.SaveChangesAsync();
            _audit.Log("Release", "Approve", $"Release approved for body #{rec.BodyId}");
            await LoadDataAsync();
            ToastHelper.ShowSuccess("Release approved.");
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to approve release: {ex.Message}");
        }
    }
}
