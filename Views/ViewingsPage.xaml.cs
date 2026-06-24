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

public partial class ViewingsPage : UserControl
{
    private readonly AuditService _audit = new();

    public ViewingsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            CboBody.ItemsSource = await db.Bodies.Where(b => b.Status != "released").ToListAsync();
            CboBody.DisplayMemberPath = "DeceasedName";
            DgViewings.ItemsSource = await db.BodyViewings.Include(v => v.Body)
                .OrderByDescending(v => v.ScheduledAt).ToListAsync();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load: {ex.Message}");
        }
    }

    private async void BtnSchedule_Click(object sender, RoutedEventArgs e)
    {
        if (CboBody.SelectedItem is not MortuaryBody body || string.IsNullOrWhiteSpace(TxtRequestedBy.Text))
        {
            ToastHelper.ShowWarning("Select body and enter requester name.");
            return;
        }

        var phone = TxtPhone.FullNumber;

        using var db = new MortuaryDbContext();
        db.BodyViewings.Add(new BodyViewing
        {
            BodyId = body.Id,
            RequestedBy = TxtRequestedBy.Text.Trim(),
            ContactPhone = phone,
            Relationship = TxtRelationship.Text.Trim(),
            ScheduledAt = DateTime.Now,
            Status = "scheduled"
        });
        await db.SaveChangesAsync();
        _audit.Log("Viewings", "Schedule", $"Viewing scheduled for {body.MortuaryNumber}");
        await LoadDataAsync();
        TxtRequestedBy.Clear(); TxtPhone.Clear(); TxtRelationship.Clear();
    }

    private async void BtnComplete_Click(object sender, RoutedEventArgs e)
    {
        if (DgViewings.SelectedItem is not BodyViewing v) return;
        using var db = new MortuaryDbContext();
        var viewing = await db.BodyViewings.FindAsync(v.Id);
        if (viewing == null) return;
        viewing.Status = "completed";
        viewing.EndedAt = DateTime.Now;
        await db.SaveChangesAsync();
        _audit.Log("Viewings", "Complete", $"Viewing #{v.Id} completed");
        await LoadDataAsync();
    }

    private async void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DgViewings.SelectedItem is not BodyViewing v) return;
        using var db = new MortuaryDbContext();
        var viewing = await db.BodyViewings.FindAsync(v.Id);
        if (viewing == null) return;
        viewing.Status = "cancelled";
        await db.SaveChangesAsync();
        await LoadDataAsync();
    }
}
