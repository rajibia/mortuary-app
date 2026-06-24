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

    public partial class ContactsPage : UserControl
    {
        private readonly AuditService _audit = new();
        private int? _editingContactId;

        public ContactsPage()
        {
            InitializeComponent();
            EmailHelper.WireValidation(TxtEmail);
            _ = LoadDataAsync();
        }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            var nextOfKins = await db.NextOfKins
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Name,
                    n.Relationship,
                    Phone = n.Phone ?? "",
                    n.Email,
                    Source = "Next of Kin",
                    BodyRef = n.Bodies.Any() ? n.Bodies.First().MortuaryNumber : ""
                })
                .ToListAsync();

            var depositors = await db.Bodies
                .Where(b => !string.IsNullOrEmpty(b.DepositorName))
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    Name = b.DepositorName ?? "",
                    Relationship = b.DepositorRelationship ?? "Depositor",
                    Phone = b.DepositorPhone ?? "",
                    Email = b.DepositorAddress ?? "",
                    Source = "Depositor",
                    BodyRef = b.MortuaryNumber
                })
                .ToListAsync();

            DgContacts.ItemsSource = nextOfKins.Concat(depositors).ToList();
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to load: {ex.Message}"); }
    }

    private void ClearForm()
    {
        TxtName.Clear(); TxtRelationship.Clear(); TxtPhone.Clear();
        TxtEmail.Clear(); TxtAddress.Clear(); TxtNationalId.Clear();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgContacts.SelectedItem is NextOfKin kin)
        {
            TxtName.Text = kin.Name;
            TxtRelationship.Text = kin.Relationship;
            TxtPhone.Text = kin.Phone ?? "";
            TxtEmail.Text = kin.Email ?? "";
            TxtAddress.Text = kin.Address ?? "";
            TxtNationalId.Text = kin.NationalId ?? "";
            _editingContactId = kin.Id;
            BtnAdd.Content = "Update";
            TxtName.Focus();
        }
        else
        {
            ToastHelper.ShowWarning("Depositor contacts can only be edited on the Admission page.");
        }
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            ToastHelper.ShowWarning("Name is required.");
            return;
        }

        var phone = TxtPhone.FullNumber;
        var email = TxtEmail.Text.Trim();

        if (!string.IsNullOrEmpty(email) && !EmailHelper.IsValidEmail(email))
        {
            ToastHelper.ShowWarning("Enter a valid email address.");
            return;
        }

        try
        {
            using var db = new MortuaryDbContext();

            if (_editingContactId.HasValue)
            {
                var kin = await db.NextOfKins.FindAsync(_editingContactId.Value);
                if (kin == null) { ToastHelper.ShowWarning("Contact not found."); return; }

                kin.Name = TxtName.Text.Trim();
                kin.Relationship = TxtRelationship.Text.Trim();
                kin.Phone = phone;
                kin.Email = email;
                kin.Address = TxtAddress.Text.Trim();
                kin.NationalId = TxtNationalId.Text.Trim();

                _audit.Log("Contacts", "Update", $"Updated next of kin {kin.Name}");
            }
            else
            {
                db.NextOfKins.Add(new NextOfKin
                {
                    Name = TxtName.Text.Trim(),
                    Relationship = TxtRelationship.Text.Trim(),
                    Phone = phone,
                    Email = email,
                    Address = TxtAddress.Text.Trim(),
                    NationalId = TxtNationalId.Text.Trim()
                });
                _audit.Log("Contacts", "Create", $"Added next of kin {TxtName.Text}");
            }

            await db.SaveChangesAsync();
            await LoadDataAsync();
            ClearForm();
            _editingContactId = null;
            BtnAdd.Content = "+ Add";
        }
        catch (Exception ex) { ToastHelper.ShowError($"Failed to save contact: {ex.Message}"); }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgContacts.SelectedItem is NextOfKin kin)
        {
            var result = MessageBox.Show($"Delete {kin.Name}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new MortuaryDbContext();
                var entity = await db.NextOfKins.FindAsync(kin.Id);
                if (entity != null) db.NextOfKins.Remove(entity);
                await db.SaveChangesAsync();
                _audit.Log("Contacts", "Delete", $"Deleted next of kin {kin.Name}");
                await LoadDataAsync();
            }
            catch (Exception ex) { ToastHelper.ShowError($"Failed to delete contact: {ex.Message}"); }
        }
        else
        {
            ToastHelper.ShowWarning("Depositor contacts can only be deleted on the Admission page.");
        }
    }
}
