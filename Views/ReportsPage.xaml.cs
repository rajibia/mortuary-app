using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Services;

namespace MortuaryApp.Views;

public partial class ReportsPage : UserControl
{
    private sealed record ReportCard(string Icon, string Title, string Description, string ReportType);

    private sealed record OrgInfo(string Name, string Address, string Phone, string Email, string LogoPath);

    private string _selectedType = "";
    private DataTable? _currentReport;
    private readonly ReportService _report = new();
    private bool _isLoading;
    private static readonly SolidColorBrush AmberBrush = new(Color.FromRgb(0xF5, 0x9E, 0x0B));

    private static readonly TimeZoneInfo GhanaTz = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");

    public ReportsPage()
    {
        InitializeComponent();
        LoadCards();
        DpFrom.SelectedDate = DateTime.Today.AddYears(-1);
        DpTo.SelectedDate = DateTime.Today;
    }

    private static DateTime GhanaNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GhanaTz);

    private void LoadCards()
    {
        (string title, string desc, string type, PackIconKind icon)[] reports = [
            ("Admission Report", "View admission records within a date range", "Admission Report", PackIconKind.ClipboardTextOutline),
            ("Financial Report", "View payments, invoices and financial summaries", "Financial Report", PackIconKind.CashMultiple),
            ("Storage Report", "View cold room occupancy and storage details", "Storage Report", PackIconKind.FridgeIndustrialOutline),
            ("Embalming Report", "View embalming records and chemical usage", "Embalming Report", PackIconKind.FlaskOutline),
            ("Release Report", "View body release records and documentation", "Release Report", PackIconKind.DoorOpen),
            ("Certificate Report", "View death and burial certificate records", "Certificate Report", PackIconKind.Certificate),
            ("Viewing Report", "View funeral viewing schedules and attendance", "Viewing Report", PackIconKind.EyeOutline),
            ("Cremation Report", "View cremation records and ash return details", "Cremation Report", PackIconKind.Fire)
        ];

        foreach (var r in reports)
        {
            var card = new Border { Width = 220, Height = 130, Margin = new Thickness(0, 0, 15, 15), Cursor = Cursors.Hand, Tag = r.type };
            card.SetResourceReference(Border.StyleProperty, "Card");
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15) };
            stack.Children.Add(new PackIcon { Kind = r.icon, Width = 24, Height = 24, Foreground = AmberBrush });
            var ttl = new TextBlock { Text = r.title, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) };
            ttl.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            stack.Children.Add(ttl);
            var dsc = new TextBlock { Text = r.desc, FontSize = 10, TextWrapping = TextWrapping.Wrap };
            dsc.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            stack.Children.Add(dsc);
            card.Child = stack;
            card.MouseLeftButtonUp += (_, _) => SelectCard(new ReportCard("", r.title, r.desc, r.type));
            CardsContainer.Children.Add(card);
        }
    }

    private void SelectCard(ReportCard card)
    {
        _selectedType = card.ReportType;
        TxtDetailTitle.Text = card.Title;
        CardsContainer.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;
        _ = LoadReportAsync();
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        CardsContainer.Visibility = Visibility.Visible;
        _currentReport = null;
        DgReport.ItemsSource = null;
        DgReport.Columns.Clear();
    }

    private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            var from = DpFrom.SelectedDate ?? DateTime.Today.AddYears(-1);
            var to = (DpTo.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            _currentReport = _selectedType switch
            {
                "Admission Report" => await Task.Run(() => _report.GetAdmissionReport(from, to)),
                "Financial Report" => await Task.Run(() => _report.GetFinancialReport(from, to)),
                "Storage Report" => await Task.Run(() => _report.GetStorageReport()),
                "Embalming Report" => await Task.Run(() => _report.GetEmbalmingReport(from, to)),
                "Release Report" => await Task.Run(() => _report.GetReleaseReport(from, to)),
                "Certificate Report" => await Task.Run(() => _report.GetCertificateReport(from, to)),
                "Viewing Report" => await Task.Run(() => _report.GetViewingReport(from, to)),
                "Cremation Report" => await Task.Run(() => _report.GetCremationReport(from, to)),
                _ => await Task.Run(() => _report.GetAdmissionReport(from, to))
            };

            DgReport.ItemsSource = null;
            DgReport.Columns.Clear();
            if (_currentReport != null)
            {
                foreach (DataColumn col in _currentReport.Columns)
                {
                    DgReport.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = col.ColumnName,
                        Binding = new System.Windows.Data.Binding(col.ColumnName),
                        Width = DataGridLength.Auto
                    });
                }
                DgReport.ItemsSource = _currentReport.DefaultView;
            }
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Report failed: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static async Task<OrgInfo> LoadOrgAsync()
    {
        using var db = new MortuaryDbContext();
        var name = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgName"))?.Value ?? "";
        var addr = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgAddress"))?.Value ?? "";
        var phone = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgPhone"))?.Value ?? "";
        var email = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgEmail"))?.Value ?? "";
        var logo = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgLogoPath"))?.Value ?? "";
        return new OrgInfo(name, addr, phone, email, logo);
    }

    private string BuildCsvHeader(OrgInfo org) =>
        $"# {org.Name}\r\n# {org.Address}\r\n# Phone: {org.Phone}\r\n# Email: {org.Email}\r\n# Generated: {GhanaNow:yyyy-MM-dd HH:mm:ss} (GMT)\r\n# Report: {_selectedType}\r\n# Period: {DpFrom.SelectedDate:yyyy-MM-dd} to {DpTo.SelectedDate:yyyy-MM-dd}\r\n# Powered By Msoft Ghana (www.msoftghana.com)\r\n";

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentReport == null || _currentReport.Rows.Count == 0)
            {
                ToastHelper.ShowWarning("Generate a report first.");
                return;
            }
            var org = await LoadOrgAsync();
            var header = BuildCsvHeader(org);
            var csv = _report.ExportToCsv(_currentReport);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"{_selectedType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv"
            };
            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(dialog.FileName, header + csv);
                ToastHelper.ShowSuccess($"Exported to {dialog.FileName}");
            }
        }
        catch (Exception ex) { ToastHelper.ShowError($"Export failed: {ex.Message}"); }
    }

    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentReport == null || _currentReport.Rows.Count == 0)
            {
                ToastHelper.ShowWarning("Generate a report first.");
                return;
            }
            ExportToPdf();
        }
        catch (Exception ex) { ToastHelper.ShowError($"PDF export failed: {ex.Message}"); }
    }

    private async void ExportToPdf()
    {
        var org = await LoadOrgAsync();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"{_selectedType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var doc = new PdfSharp.Pdf.PdfDocument();
            doc.Info.Title = _selectedType;

            PdfSharp.Drawing.XGraphics? gfx = null;
            try
            {
                var page = doc.AddPage();
                page.Size = PdfSharp.PageSize.Letter;
                page.Orientation = PdfSharp.PageOrientation.Landscape;

                gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                var y = 25d;
                var margin = 35d;
                var pageWidth = page.Width;
                var contentWidth = pageWidth - margin * 2;

                var bold9 = new PdfSharp.Drawing.XFont("Segoe UI", 9, PdfSharp.Drawing.XFontStyleEx.Bold);
                var reg8 = new PdfSharp.Drawing.XFont("Segoe UI", 8, PdfSharp.Drawing.XFontStyleEx.Regular);
                var headerFont = new PdfSharp.Drawing.XFont("Segoe UI", 9, PdfSharp.Drawing.XFontStyleEx.Bold);
                var cellFont = new PdfSharp.Drawing.XFont("Segoe UI", 8, PdfSharp.Drawing.XFontStyleEx.Regular);
                var titleFont = new PdfSharp.Drawing.XFont("Segoe UI", 16, PdfSharp.Drawing.XFontStyleEx.Bold);

                // --- Org Header ---
                if (!string.IsNullOrEmpty(org.Name))
                {
                    gfx.DrawString(org.Name, titleFont, PdfSharp.Drawing.XBrushes.Black, margin, y + 16);
                    y += 10;
                }

                if (!string.IsNullOrEmpty(org.LogoPath) && File.Exists(org.LogoPath))
                {
                    try { var img = PdfSharp.Drawing.XImage.FromFile(org.LogoPath); gfx.DrawImage(img, margin, y, 60, 60); y += 65; }
                    catch { }
                }

                var ghanaTime = GhanaNow;
                gfx.DrawString(ghanaTime.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt"), reg8, PdfSharp.Drawing.XBrushes.DimGray, margin, y + 12);
                y += 8;
                if (!string.IsNullOrEmpty(org.Address)) { gfx.DrawString(org.Address, reg8, PdfSharp.Drawing.XBrushes.DimGray, margin, y + 12); y += 8; }
                if (!string.IsNullOrEmpty(org.Phone)) { gfx.DrawString($"Phone: {org.Phone}", reg8, PdfSharp.Drawing.XBrushes.DimGray, margin, y + 12); y += 8; }
                if (!string.IsNullOrEmpty(org.Email)) { gfx.DrawString($"Email: {org.Email}", reg8, PdfSharp.Drawing.XBrushes.DimGray, margin, y + 12); y += 8; }

                // Separator
                y += 6;
                var sepPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(180, 180, 180), 0.8);
                gfx.DrawLine(sepPen, margin, y, margin + contentWidth, y);
                y += 10;

                // Report Title & Period
                gfx.DrawString(_selectedType, bold9, PdfSharp.Drawing.XBrushes.Black, margin, y + 12);
                y += 16;
                gfx.DrawString($"Period: {DpFrom.SelectedDate:yyyy-MM-dd} to {DpTo.SelectedDate:yyyy-MM-dd}",
                    reg8, PdfSharp.Drawing.XBrushes.Gray, margin, y + 11);
                y += 18;

                // --- Data Table ---
                if (_currentReport != null && _currentReport.Columns.Count > 0)
                {
                    var colCount = _currentReport.Columns.Count;
                    var colWidth = contentWidth / colCount;
                    var headerBrush = PdfSharp.Drawing.XBrushes.DimGray;
                    var borderPen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(200, 200, 200), 0.5);

                    for (int c = 0; c < colCount; c++)
                    {
                        var x = margin + c * colWidth;
                        gfx.DrawString(_currentReport.Columns[c].ColumnName, headerFont, headerBrush, x + 3, y + 13);
                    }
                    y += 20;
                    gfx.DrawLine(borderPen, margin, y, margin + contentWidth, y);
                    y += 4;

                    foreach (DataRow row in _currentReport.Rows)
                    {
                        if (y > page.Height - 60)
                        {
                            var pg = doc.AddPage();
                            pg.Size = PdfSharp.PageSize.Letter;
                            pg.Orientation = PdfSharp.PageOrientation.Landscape;
                            gfx.Dispose();
                            gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pg);
                            y = 30;
                        }

                        for (int c = 0; c < colCount; c++)
                        {
                            var x = margin + c * colWidth;
                            var val = row[c]?.ToString() ?? "";
                            gfx.DrawString(val, cellFont, PdfSharp.Drawing.XBrushes.Black, x + 3, y + 11);
                        }
                        y += 16;
                    }
                }

                // --- Footer ---
                y = Math.Max(y + 15, page.Height - 45);
                var finePen = new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColor.FromArgb(180, 180, 180), 0.5);
                gfx.DrawLine(finePen, margin, y, margin + contentWidth, y);
                y += 5;
                var footerFont = new PdfSharp.Drawing.XFont("Segoe UI", 7, PdfSharp.Drawing.XFontStyleEx.Italic);
                gfx.DrawString($"Generated: {ghanaTime:yyyy-MM-dd HH:mm:ss} (GMT)  |  Powered By Msoft Ghana (www.msoftghana.com)",
                    footerFont, PdfSharp.Drawing.XBrushes.Gray, margin, y + 10);
            }
            finally
            {
                gfx?.Dispose();
            }

            doc.Save(dialog.FileName);
            ToastHelper.ShowSuccess($"Exported to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF export error: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private FixedDocument BuildFixedDocument()
    {
        var doc = new FixedDocument();
        var page = new FixedPage { Width = 816, Height = 1056 };
        var root = new Grid { Margin = new Thickness(50) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var row = 0;

        var orgInfoBlock = new TextBlock { FontSize = 11, Foreground = Brushes.Black };
        orgInfoBlock.Inlines.Add(new Bold(new Run("MORGUE MANAGEMENT SYSTEM")) { FontSize = 18 });
        orgInfoBlock.Inlines.Add(new LineBreak());
        orgInfoBlock.Inlines.Add(new LineBreak());
        using (var db = new MortuaryDbContext())
        {
            var orgName = db.Settings.FirstOrDefault(s => s.Key == "OrgName")?.Value ?? "";
            var orgAddr = db.Settings.FirstOrDefault(s => s.Key == "OrgAddress")?.Value ?? "";
            var orgPhone = db.Settings.FirstOrDefault(s => s.Key == "OrgPhone")?.Value ?? "";
            var orgEmail = db.Settings.FirstOrDefault(s => s.Key == "OrgEmail")?.Value ?? "";
            if (!string.IsNullOrEmpty(orgName)) orgInfoBlock.Inlines.Add(new Run(orgName) { FontSize = 14 });
            orgInfoBlock.Inlines.Add(new LineBreak());
            var ghanaTime = GhanaNow;
            orgInfoBlock.Inlines.Add(new Run(ghanaTime.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt")) { FontSize = 10, Foreground = Brushes.Gray });
            orgInfoBlock.Inlines.Add(new LineBreak());
            if (!string.IsNullOrEmpty(orgAddr)) { orgInfoBlock.Inlines.Add(new Run(orgAddr) { FontSize = 10, Foreground = Brushes.Gray }); orgInfoBlock.Inlines.Add(new LineBreak()); }
            if (!string.IsNullOrEmpty(orgPhone)) { orgInfoBlock.Inlines.Add(new Run($"Phone: {orgPhone}") { FontSize = 10, Foreground = Brushes.Gray }); orgInfoBlock.Inlines.Add(new LineBreak()); }
            if (!string.IsNullOrEmpty(orgEmail)) { orgInfoBlock.Inlines.Add(new Run($"Email: {orgEmail}") { FontSize = 10, Foreground = Brushes.Gray }); orgInfoBlock.Inlines.Add(new LineBreak()); }
        }
        Grid.SetRow(orgInfoBlock, row++);
        root.Children.Add(orgInfoBlock);

        // Separator
        var sep = new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 10, 0, 10) };
        Grid.SetRow(sep, row++);
        root.Children.Add(sep);

        var title = new TextBlock
        {
            Text = _selectedType,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(title, row++);
        root.Children.Add(title);

        var dateLine = new TextBlock
        {
            Text = $"Period: {DpFrom.SelectedDate:yyyy-MM-dd} to {DpTo.SelectedDate:yyyy-MM-dd}",
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 15)
        };
        Grid.SetRow(dateLine, row++);
        root.Children.Add(dateLine);

        var dg = new DataGrid
        {
            ItemsSource = _currentReport?.DefaultView,
            AutoGenerateColumns = true,
            FontSize = 10,
            RowHeight = 22,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(dg, row++);
        root.Children.Add(dg);

        var footer = new TextBlock
        {
            Text = $"Generated: {GhanaNow:yyyy-MM-dd HH:mm:ss} (GMT)  |  Powered By Msoft Ghana (www.msoftghana.com)",
            FontSize = 8,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(footer, row++);
        root.Children.Add(footer);

        page.Children.Add(root);
        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(page);
        doc.Pages.Add(pageContent);
        return doc;
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentReport == null || _currentReport.Rows.Count == 0)
            {
                ToastHelper.ShowWarning("Generate a report first.");
                return;
            }
            var doc = BuildFixedDocument();
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
                dialog.PrintDocument(doc.DocumentPaginator, _selectedType);
        }
        catch (Exception ex) { ToastHelper.ShowError($"Print failed: {ex.Message}"); }
    }
}
