using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class CertificatesPage : UserControl
{
    private readonly AuditService _audit = new();

    public CertificatesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            CboBody.ItemsSource = await db.Bodies.Where(b => b.DeathCertificate == null).ToListAsync();
            CboBody.DisplayMemberPath = "DeceasedName";
            DgCertificates.ItemsSource = await db.DeathCertificates.Include(c => c.Body)
                .OrderByDescending(c => c.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnIssue_Click(object sender, RoutedEventArgs e)
    {
        if (CboBody.SelectedItem is not Models.MortuaryBody body || string.IsNullOrWhiteSpace(TxtIssuedTo.Text))
        {
            ToastHelper.ShowWarning("Select body and enter recipient.");
            return;
        }

        using var db = new MortuaryDbContext();
        db.DeathCertificates.Add(new Models.DeathCertificate
        {
            BodyId = body.Id,
            CertificateNumber = NumberGenerator.GenerateCertificateNumber(),
            IssuedTo = TxtIssuedTo.Text.Trim(),
            IdType = TxtIdType.Text.Trim(),
            IdNumber = TxtIdNumber.Text.Trim(),
            CauseOfDeath = body.CauseOfDeath,
            PlaceOfDeath = body.Source,
            Status = "issued",
            IssuedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
        _audit.Log("Certificates", "Issue", $"Certificate issued for {body.MortuaryNumber}");
        await LoadDataAsync();
        TxtIssuedTo.Clear(); TxtIdType.Clear(); TxtIdNumber.Clear();
    }
}
