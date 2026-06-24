using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using SkiaSharp;

namespace MortuaryApp.Views;

public partial class DashboardPage : UserControl
{
    private static readonly TimeZoneInfo GhanaTz = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => { await LoadDataAsync(); };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            var inStorageTask = db.Bodies.CountAsync(b => b.Status == "stored" || b.Status == "admitted");
            var totalSlotsTask = db.StorageLocations.CountAsync();
            var releasedTodayTask = db.ReleaseRecords.CountAsync(r => r.ReleasedAt.HasValue && r.ReleasedAt.Value.Date == DateTime.Today);
            var weekReleasesTask = db.ReleaseRecords.CountAsync(r => r.ReleasedAt.HasValue && r.ReleasedAt.Value >= DateTime.Today.AddDays(-7));
            var pendingPaymentsTask = db.Charges.CountAsync(c => c.Status == "pending");
            var pendingChargesTask = db.Charges.Where(c => c.Status == "pending").ToListAsync();
            var occupancyTask = db.StorageLocations.CountAsync(l => l.Status == "occupied");
            var totalCapacityTask = db.StorageLocations.SumAsync(l => (int?)l.Capacity ?? 0);

            var recentAdmissionsTask = db.Bodies.OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync();

            await Task.WhenAll(inStorageTask, totalSlotsTask, releasedTodayTask, weekReleasesTask,
                pendingPaymentsTask, pendingChargesTask, occupancyTask, totalCapacityTask, recentAdmissionsTask);

            TxtInStorage.Text = inStorageTask.Result.ToString("N0");
            var totalSlots = totalSlotsTask.Result;
            TxtStorageUsage.Text = totalSlots > 0 ? $"{inStorageTask.Result}/{totalSlots} slots" : $"{inStorageTask.Result} bodies";
            TxtReleasedToday.Text = releasedTodayTask.Result.ToString("N0");
            TxtWeekReleases.Text = $"{weekReleasesTask.Result} this week";
            TxtPendingRelease.Text = pendingPaymentsTask.Result.ToString("N0");
            var pendingAmount = (decimal?)pendingChargesTask.Result.Sum(c => c.Amount - c.PaidAmount);
            TxtPendingAmount.Text = $"₵{pendingAmount ?? 0:N0} outstanding";
            TxtOccupancy.Text = $"{occupancyTask.Result} / {totalCapacityTask.Result}";

            DgRecentAdmissions.ItemsSource = recentAdmissionsTask.Result;

            // Admissions chart
            var allBodies = await db.Bodies.ToListAsync();
            var admissionsData = allBodies
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new { Label = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count() })
                .OrderBy(x => x.Label)
                .ToList();

            AdmissionsChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = admissionsData.Select(d => d.Count).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11)),
                    Stroke = null
                }
            };
            AdmissionsChart.XAxes = new[]
            {
                new Axis { Labels = admissionsData.Select(d => d.Label).ToList(), LabelsRotation = 45, TextSize = 10 }
            };
            AdmissionsChart.YAxes = new[] { new Axis { TextSize = 10 } };

            // Occupancy ring chart
            var occupied = await db.StorageLocations.CountAsync(l => l.Status == "occupied");
            var empty = totalCapacityTask.Result - occupied;
            OccupancyChart.Series = new ISeries[]
            {
                new PieSeries<int>
                {
                    Values = new[] { occupied },
                    Name = "Occupied",
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11)),
                    Stroke = null
                },
                new PieSeries<int>
                {
                    Values = new[] { empty < 0 ? 0 : empty },
                    Name = "Available",
                    Fill = new SolidColorPaint(new SKColor(209, 213, 219)),
                    Stroke = null
                }
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load dashboard: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
