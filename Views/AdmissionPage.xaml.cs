using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Models;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class AdmissionPage : UserControl
{
    private readonly AuditService _audit = new();
    private readonly TimelineService _timeline = new();

    public AdmissionPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var storageTask = db.StorageLocations.Where(s => s.Status == "available").ToListAsync();

            if (CboBillingStartTime.Items.Count == 0)
            {
                for (int h = 0; h < 24; h++)
                {
                    var hour12 = h == 0 ? 12 : (h > 12 ? h - 12 : h);
                    var ampm = h < 12 ? "AM" : "PM";
                    CboBillingStartTime.Items.Add($"{hour12:D2}:00 {ampm}");
                    CboBillingStartTime.Items.Add($"{hour12:D2}:30 {ampm}");
                }
                CboBillingStartTime.Text = "08:00 AM";
            }

            var countTask = db.Bodies.CountAsync();
            var recentTask = db.Bodies.Include(b => b.StorageLocation)
                .OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync();

            await Task.WhenAll(storageTask, countTask, recentTask);

            CboStorage.ItemsSource = storageTask.Result;
            DgBodies.ItemsSource = recentTask.Result;

            TxtPageInfo.Text = $"{countTask.Result} total admissions";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnAdmit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Deceased name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedLocation = CboStorage.SelectedItem as StorageLocation;
        if (selectedLocation == null)
        {
            MessageBox.Show("Please select a storage location.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using (var checkDb = new MortuaryDbContext())
            {
                var occupied = await checkDb.StorageLocations
                    .Where(s => s.Id == selectedLocation.Id && s.Status == "occupied").AnyAsync();
                if (occupied)
                {
                    MessageBox.Show("Selected storage location is already occupied.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var body = new MortuaryBody
            {
                MortuaryNumber = await GenerateMortuaryNumber(),
                DeceasedName = TxtName.Text.Trim(),
                Gender = (CboGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Male",
                DateOfBirth = DpDob.SelectedDate,
                DateOfDeath = DpDod.SelectedDate,
                CauseOfDeath = TxtCause.Text.Trim(),
                Source = TxtSource.Text.Trim(),
                DepositorName = TxtDepositorName.Text.Trim(),
                DepositorAddress = TxtDepositorAddress.Text.Trim(),
                DepositorPhone = TxtDepositorPhone.Text.Trim(),
                DepositorRelationship = TxtDepositorRelationship.Text.Trim(),
                StorageLocationId = selectedLocation.Id,
                Status = "admitted",
                AmountToBePaid = decimal.TryParse(TxtAmountToBePaid.Text, out var amountDue) ? amountDue : 0,
                BillingRate = decimal.TryParse(TxtBillingRate.Text, out var rate) ? rate : 0,
                DepositAmount = decimal.TryParse(TxtDeposit.Text, out var deposit) ? deposit : 0,
                BillingType = (CboBillingType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "daily",
                BillingStartAt = ParseBillingStart(),
                AdmissionNotes = TxtNotes.Text.Trim(),
                CreatedAt = DateTime.Now
            };

            using var db = new MortuaryDbContext();
            db.Bodies.Add(body);

            var loc = await db.StorageLocations.FindAsync(selectedLocation.Id);
            if (loc != null) loc.Status = "occupied";

            await db.SaveChangesAsync();

            _timeline.AddEvent(body.Id, "Admitted", $"Body admitted at {loc?.DisplayName}", App.CurrentUser?.FullName ?? "System");
            _audit.Log("Admission", $"Admitted body '{body.DeceasedName}' ({body.MortuaryNumber})", App.CurrentUser?.FullName ?? "System");

            MessageBox.Show($"Body admitted successfully.\nMortuary #: {body.MortuaryNumber}", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string> GenerateMortuaryNumber()
    {
        using var db = new MortuaryDbContext();
        var last = await db.Bodies.OrderByDescending(b => b.Id).FirstOrDefaultAsync();
        var nextId = (last?.Id ?? 0) + 1;
        return $"MGH-{DateTime.Now:yyyyMMdd}-{nextId:D4}";
    }

    private void ClearForm()
    {
        TxtName.Clear();
        CboGender.SelectedIndex = -1;
        DpDob.SelectedDate = null;
        DpDod.SelectedDate = null;
        TxtCause.Clear();
        TxtSource.Clear();
        TxtDepositorName.Clear();
            TxtDepositorAddress.Clear();
            TxtDepositorPhone.Clear();
        TxtDepositorRelationship.Clear();
        CboStorage.SelectedIndex = -1;
        CboBillingType.SelectedIndex = -1;
        TxtAmountToBePaid.Text = "500.00";
        TxtBillingRate.Text = "120.00";
        TxtDeposit.Text = "0";
        DpBillingStart.SelectedDate = null;
        CboBillingStartTime.Text = "08:00 AM";
        TxtNotes.Clear();
    }

    private DateTime? ParseBillingStart()
    {
        if (DpBillingStart.SelectedDate == null) return null;
        var date = DpBillingStart.SelectedDate.Value.Date;
        var timeText = CboBillingStartTime.Text.Trim();
        if (DateTime.TryParse(timeText, out var time))
            return date.Add(time.TimeOfDay);
        return date;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Clear all form fields?", "Confirm", 
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            ClearForm();
    }

    private void DgBodies_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgBodies.SelectedItem is MortuaryBody body)
        {
            TxtName.Text = body.DeceasedName;
            DpDob.SelectedDate = body.DateOfBirth;
            DpDod.SelectedDate = body.DateOfDeath;
            TxtCause.Text = body.CauseOfDeath;
            TxtSource.Text = body.Source;
            TxtDepositorName.Text = body.DepositorName ?? "";
            TxtDepositorAddress.Text = body.DepositorAddress ?? "";
            TxtDepositorPhone.Text = body.DepositorPhone ?? "";
            TxtDepositorRelationship.Text = body.DepositorRelationship ?? "";
        }
    }
}
