using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MortuaryApp.Views;

public partial class ReceiptWindow : Window
{
    private readonly int _chargeId;
    private readonly decimal _amountPaid;
    private readonly decimal _changeGiven;
    private string _orgName = "";
    private string _orgAddr = "";
    private string _orgPhone = "";
    private string _orgEmail = "";
    private string _orgLogoPath = "";

    private static readonly TimeZoneInfo GhanaTz = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");
    private static DateTime GhanaNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GhanaTz);

    public ReceiptWindow(int chargeId, decimal amountPaid = 0, decimal changeGiven = 0)
    {
        InitializeComponent();
        _chargeId = chargeId;
        _amountPaid = amountPaid;
        _changeGiven = changeGiven;
        Loaded += async (_, _) => await LoadReceiptAsync();
    }

    private async Task LoadReceiptAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();

            _orgLogoPath = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgLogoPath"))?.Value ?? "";

            var charge = await db.Charges
                .Include(c => c.Body)
                .FirstAsync(c => c.Id == _chargeId);

            _orgName = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgName"))?.Value ?? "Mortuary Services";
            _orgAddr = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgAddress"))?.Value ?? "";
            _orgPhone = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgPhone"))?.Value ?? "";
            _orgEmail = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgEmail"))?.Value ?? "";

            TxtOrgName.Text = _orgName;
            TxtAddress.Text = _orgAddr;
            TxtAddress.Visibility = string.IsNullOrWhiteSpace(_orgAddr) ? Visibility.Collapsed : Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(_orgPhone) && !string.IsNullOrWhiteSpace(_orgEmail))
                TxtPhone.Text = $"{_orgPhone}";
            else if (!string.IsNullOrWhiteSpace(_orgPhone))
                TxtPhone.Text = _orgPhone;
            else
                TxtPhone.Visibility = Visibility.Collapsed;

            TxtEmail.Text = _orgEmail;
            TxtEmail.Visibility = string.IsNullOrWhiteSpace(_orgEmail) ? Visibility.Collapsed : Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(_orgLogoPath) && File.Exists(_orgLogoPath))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(Path.GetFullPath(_orgLogoPath));
                img.EndInit();
                ImgLogo.Source = img;
                LogoBorder.Visibility = Visibility.Visible;
            }

            var paidAt = charge.PaidAt.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(charge.PaidAt.Value.ToUniversalTime(), GhanaTz)
                : GhanaNow;

            TxtReceiptNo.Text = $"RCP-{charge.Id:D5}";
            TxtDate.Text = paidAt.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt") + " (GMT)";
            TxtCashier.Text = App.CurrentUser?.FullName ?? "Unknown";

            TxtMortuaryNo.Text = charge.Body?.MortuaryNumber ?? "—";
            TxtDeceased.Text = charge.Body?.DeceasedName ?? "—";

            var actualPaid = _amountPaid > 0 ? _amountPaid : charge.PaidAmount;
            var change = _amountPaid > 0 ? _changeGiven : 0;

            TxtDescription.Text = charge.Description;
            TxtAmount.Text = $"\u20B5{charge.Amount:N2}";
            TxtPaidAmount.Text = $"\u20B5{actualPaid:N2}";
            TxtChange.Text = change > 0 ? $"\u20B5{change:N2}" : "\u20B50.00";
            TxtBalance.Text = $"\u20B5{charge.Balance:N2}";

            if (charge.Status == "paid")
            {
                TxtStatus.Text = "PAID IN FULL";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (charge.Status == "partial")
            {
                TxtStatus.Text = "PARTIALLY PAID";
                TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                TxtStatus.Text = charge.Status.ToUpper();
                TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }

            TxtTotalPaid.Text = $"\u20B5{actualPaid:N2}";
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Failed to load receipt: {ex.Message}");
        }
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(ReceiptPanel, $"Receipt RCP-{_chargeId:D5}");
                ToastHelper.ShowSuccess("Receipt printed successfully!");
                Close();
            }
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"Print failed: {ex.Message}");
        }
    }

    private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"Receipt_RCP{_chargeId:D5}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var doc = new PdfDocument();
            doc.Info.Title = $"Receipt RCP-{_chargeId:D5}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.Letter;
            page.Orientation = PdfSharp.PageOrientation.Portrait;

            using var gfx = XGraphics.FromPdfPage(page);
            var y = 25d;
            var margin = 40d;
            var pageWidth = page.Width;
            var contentWidth = pageWidth - margin * 2;

            var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
            var reg9 = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var bold9 = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            var reg8 = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
            var bold8 = new XFont("Segoe UI", 8, XFontStyleEx.Bold);

            // Org Header
            if (!string.IsNullOrEmpty(_orgName))
            {
                gfx.DrawString(_orgName, titleFont, XBrushes.Black, margin, y + 16);
                y += 10;
            }

            if (!string.IsNullOrEmpty(_orgLogoPath) && File.Exists(_orgLogoPath))
            {
                try { var img = XImage.FromFile(_orgLogoPath); gfx.DrawImage(img, margin, y, 55, 55); y += 60; }
                catch { }
            }

            var ghanaTime = GhanaNow;
            gfx.DrawString(ghanaTime.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt"), reg9, XBrushes.DimGray, margin, y + 12);
            y += 8;
            if (!string.IsNullOrEmpty(_orgAddr)) { gfx.DrawString(_orgAddr, reg9, XBrushes.DimGray, margin, y + 12); y += 8; }
            if (!string.IsNullOrEmpty(_orgPhone)) { gfx.DrawString($"Phone: {_orgPhone}", reg9, XBrushes.DimGray, margin, y + 12); y += 8; }
            if (!string.IsNullOrEmpty(_orgEmail)) { gfx.DrawString($"Email: {_orgEmail}", reg9, XBrushes.DimGray, margin, y + 12); y += 8; }

            // Separator
            y += 6;
            var sepPen = new XPen(XColor.FromArgb(180, 180, 180), 0.8);
            gfx.DrawLine(sepPen, margin, y, margin + contentWidth, y);
            y += 12;

            // Receipt Title
            gfx.DrawString("PAYMENT RECEIPT", new XFont("Segoe UI", 14, XFontStyleEx.Bold), XBrushes.Black, margin, y + 14);
            y += 22;

            // Receipt details
            var labelX = margin;
            var valueX = margin + 100;

            var receiptNo = TxtReceiptNo.Text;
            var dateStr = TxtDate.Text;
            var cashier = TxtCashier.Text;

            gfx.DrawString("Receipt #:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(receiptNo, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Date/Time:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(dateStr, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Cashier:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(cashier, bold9, XBrushes.Black, valueX, y + 11);
            y += 20;

            gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200), 0.5), margin, y, margin + contentWidth, y);
            y += 10;

            // Deceased Info
            gfx.DrawString("DECEASED INFORMATION", bold8, XBrushes.Gray, margin, y + 10);
            y += 18;
            gfx.DrawString("Mortuary #:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtMortuaryNo.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Deceased:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtDeceased.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 20;

            // Charge Details
            gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200), 0.5), margin, y, margin + contentWidth, y);
            y += 10;
            gfx.DrawString("CHARGE DETAILS", bold8, XBrushes.Gray, margin, y + 10);
            y += 18;
            gfx.DrawString("Description:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtDescription.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Amount Due:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtAmount.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Amount Paid:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtPaidAmount.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Change:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtChange.Text, bold9, new XSolidBrush(XColor.FromArgb(5, 153, 105)), valueX, y + 11);
            y += 16;
            gfx.DrawString("Balance:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtBalance.Text, bold9, XBrushes.Black, valueX, y + 11);
            y += 16;
            gfx.DrawString("Status:", reg9, XBrushes.DimGray, labelX, y + 11);
            gfx.DrawString(TxtStatus.Text, bold9, new XSolidBrush(XColor.FromArgb(22, 163, 74)), valueX, y + 11);
            y += 20;

            // Total Paid
            gfx.DrawLine(sepPen, margin, y, margin + contentWidth, y);
            y += 12;
            var greenBrush = new XSolidBrush(XColor.FromArgb(22, 163, 74));
            gfx.DrawString("TOTAL PAID", new XFont("Segoe UI", 12, XFontStyleEx.Bold), greenBrush, margin, y + 14);
            gfx.DrawString(TxtTotalPaid.Text, new XFont("Segoe UI", 16, XFontStyleEx.Bold), greenBrush, margin + contentWidth - 80, y + 14);

            // Footer
            y = Math.Max(y + 20, page.Height - 40);
            gfx.DrawLine(new XPen(XColor.FromArgb(180, 180, 180), 0.5), margin, y, margin + contentWidth, y);
            y += 5;
            gfx.DrawString($"Generated: {ghanaTime:yyyy-MM-dd HH:mm:ss} (GMT)  |  Powered By Msoft Ghana (www.msoftghana.com)",
                new XFont("Segoe UI", 7, XFontStyleEx.Italic), XBrushes.Gray, margin, y + 10);

            doc.Save(dialog.FileName);
            ToastHelper.ShowSuccess("Receipt exported to PDF successfully!");
            Close();
        }
        catch (Exception ex)
        {
            ToastHelper.ShowError($"PDF export failed: {ex.Message}");
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
