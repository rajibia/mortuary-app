using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Helpers;
using MortuaryApp.Services;
using MortuaryApp.Views;

namespace MortuaryApp;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer = new();
    private static readonly TimeZoneInfo GhanaTz = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            _ = RefreshOrgNameAsync();
            MainFrame.Navigated += (_, _) =>
            {
                if (MainFrame.Content is FrameworkElement page)
                {
                    page.Opacity = 0;
                    page.RenderTransform = new TranslateTransform { Y = 10 };

                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var slideUp = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    var sb = new Storyboard();
                    Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                    Storyboard.SetTargetProperty(slideUp, new PropertyPath("RenderTransform.Y"));
                    sb.Children.Add(fadeIn);
                    sb.Children.Add(slideUp);
                    sb.Begin(page);
                }
            };
            ThemeIcon.Kind = App.IsDarkTheme
                ? MaterialDesignThemes.Wpf.PackIconKind.WeatherNight
                : MaterialDesignThemes.Wpf.PackIconKind.WeatherSunny;
            SetActive(NavDashboard);
            MainFrame.Navigate(new DashboardPage());
            PageTitle.Text = "Dashboard";
            PageSubtitle.Text = "Overview of mortuary operations";

            var user = App.CurrentUser;
            if (user != null)
            {
                TxtUserName.Text = user.FullName;
                TxtUserRole.Text = char.ToUpper(user.Role[0]) + user.Role[1..];
                var parts = user.FullName.Split(' ');
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[^1][0]}"
                    : user.FullName[..1].ToUpper();
                TxtUserAvatar.Text = initials;
            }

            _ = ApplyPermissionsAsync();

            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (_, _) =>
                ClockText.Text = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GhanaTz)
                    .ToString("ddd, dd MMM yyyy  HH:mm:ss");
            _clockTimer.Start();
            ClockText.Text = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GhanaTz)
                .ToString("ddd, dd MMM yyyy  HH:mm:ss");

            TxtGlobalSearch.KeyDown += TxtGlobalSearch_KeyDown;
            Closed += (_, _) => Application.Current.Shutdown();

            var desktopDim = SystemParameters.WorkArea;
            Left = (desktopDim.Width - Width) / 2;
            Top = (desktopDim.Height - Height) / 2;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n\n{ex.GetType()}\n\n{ex.StackTrace}", "MainWindow Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetActive(Button activeBtn)
    {
        foreach (var child in NavPanel.Children)
        {
            if (child is Button btn)
            {
                btn.IsEnabled = true;
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(139, 149, 165));
                btn.BorderThickness = new Thickness(0);
                btn.FontWeight = FontWeights.Normal;
            }
        }
        activeBtn.IsEnabled = false;
        activeBtn.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        activeBtn.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        activeBtn.FontWeight = FontWeights.SemiBold;
        activeBtn.BorderThickness = new Thickness(3, 0, 0, 0);
        activeBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
    }

    private Button? FindNavButton(string tag)
    {
        foreach (var child in NavPanel.Children)
            if (child is Button btn && btn.Tag?.ToString() == tag) return btn;
        return null;
    }

    public void NavigateTo(string tag)
    {
        var navBtn = FindNavButton(tag);
        if (navBtn != null) SetActive(navBtn);

        switch (tag)
        {
            case "Dashboard": MainFrame.Navigate(new DashboardPage()); PageTitle.Text = "Dashboard"; PageSubtitle.Text = "Overview of mortuary operations"; break;
            case "Admission": MainFrame.Navigate(new Views.AdmissionPage()); PageTitle.Text = "Body Admission"; PageSubtitle.Text = "Register a new deceased person"; break;
            case "BodyLocator": MainFrame.Navigate(new Views.BodyLocatorPage()); PageTitle.Text = "Body Locator"; PageSubtitle.Text = "Find where bodies are stored"; break;
            case "ColdRoom": MainFrame.Navigate(new Views.ColdRoomPage()); PageTitle.Text = "Cold Room"; PageSubtitle.Text = "Manage cold storage rooms"; break;
            case "Embalming": MainFrame.Navigate(new Views.EmbalmingPage()); PageTitle.Text = "Embalming"; PageSubtitle.Text = "Track embalming procedures"; break;
            case "Release": MainFrame.Navigate(new Views.ReleasePage()); PageTitle.Text = "Body Release"; PageSubtitle.Text = "Process body releases"; break;
            case "Viewings": MainFrame.Navigate(new Views.ViewingsPage()); PageTitle.Text = "Viewings"; PageSubtitle.Text = "Schedule and manage viewings"; break;
            case "Cremations": MainFrame.Navigate(new Views.CremationsPage()); PageTitle.Text = "Cremations"; PageSubtitle.Text = "Manage cremation requests"; break;
            case "Billing": MainFrame.Navigate(new Views.BillingPage()); PageTitle.Text = "Billing"; PageSubtitle.Text = "Invoices and payments"; break;
            case "Incomes": MainFrame.Navigate(new Views.IncomesPage()); PageTitle.Text = "Incomes"; PageSubtitle.Text = "Track income entries"; break;
            case "Expenses": MainFrame.Navigate(new Views.ExpensesPage()); PageTitle.Text = "Expenses"; PageSubtitle.Text = "Track expense entries"; break;
            case "Revenue": MainFrame.Navigate(new Views.RevenuePage()); PageTitle.Text = "Revenue"; PageSubtitle.Text = "Net revenue overview"; break;
            case "Contacts": MainFrame.Navigate(new Views.ContactsPage()); PageTitle.Text = "Contacts"; PageSubtitle.Text = "Family and next-of-kin contacts"; break;
            case "Certificates": MainFrame.Navigate(new Views.CertificatesPage()); PageTitle.Text = "Certificates"; PageSubtitle.Text = "Death certificates and documents"; break;
            case "Temperature": MainFrame.Navigate(new Views.TemperaturePage()); PageTitle.Text = "Temperature Log"; PageSubtitle.Text = "Cold room temperature readings"; break;
            case "Inventory": MainFrame.Navigate(new Views.InventoryPage()); PageTitle.Text = "Inventory"; PageSubtitle.Text = "Track supplies and equipment"; break;
            case "Reports": MainFrame.Navigate(new Views.ReportsPage()); PageTitle.Text = "Reports"; PageSubtitle.Text = "Generate system reports"; break;
            case "Audit": MainFrame.Navigate(new Views.AuditPage()); PageTitle.Text = "Audit Trail"; PageSubtitle.Text = "System activity log"; break;
        case "Settings":     MainFrame.Navigate(new Views.SettingsPage());   PageTitle.Text = "Settings";            PageSubtitle.Text = "System configuration"; break;
        case "HumanResource": MainFrame.Navigate(new Views.HumanResourcePage()); PageTitle.Text = "Human Resource"; PageSubtitle.Text = "Manage system users and accounts"; break;
        }
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            NavigateTo(tag);
    }

    private void TxtGlobalSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !string.IsNullOrWhiteSpace(TxtGlobalSearch.Text))
        {
            SetActive(NavDashboard);
            PageTitle.Text = $"Search: {TxtGlobalSearch.Text}";
            PageSubtitle.Text = "Search results";
            MainFrame.Navigate(new Views.BodyLocatorPage(TxtGlobalSearch.Text.Trim()));
        }
    }

    private void UserInfo_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var result = MessageBox.Show($"Signed in as {App.CurrentUser?.FullName}\n\nClick Yes to logout.", "Logout",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        var login = new LoginWindow();
        Hide();
        if (login.ShowDialog() == true)
        {
            App.CurrentUser = login.AuthenticatedUser;
            TxtUserName.Text = App.CurrentUser?.FullName ?? "";
            TxtUserRole.Text = App.CurrentUser != null
                ? char.ToUpper(App.CurrentUser.Role[0]) + App.CurrentUser.Role[1..]
                : "";
            if (App.CurrentUser != null)
            {
                var parts = App.CurrentUser.FullName.Split(' ');
                TxtUserAvatar.Text = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[^1][0]}"
                    : App.CurrentUser.FullName[..1].ToUpper();
            }
            SetActive(NavDashboard);
            MainFrame.Navigate(new DashboardPage());
            PageTitle.Text = "Dashboard";
            PageSubtitle.Text = "Overview of mortuary operations";
            Show();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    public async Task RefreshCurrentUserAsync()
    {
        using var db = new MortuaryDbContext();
        var user = await db.Users.FindAsync(App.CurrentUser?.Id);
        if (user != null)
        {
            App.CurrentUser = user;
            TxtUserName.Text = user.FullName;
            TxtUserRole.Text = char.ToUpper(user.Role[0]) + user.Role[1..];
            var parts = user.FullName.Split(' ');
            var initials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}"
                : user.FullName[..1].ToUpper();
            TxtUserAvatar.Text = initials;
        }
    }

    private async Task ApplyPermissionsAsync()
    {
        await PermissionService.LoadAsync();

        var visibility = new Dictionary<string, Button>
        {
            ["pages.dashboard"] = NavDashboard,
            ["pages.admission"] = NavAdmission,
            ["pages.bodies"] = NavBodyLocator,
            ["pages.coldroom"] = NavColdRoom,
            ["pages.embalming"] = NavEmbalming,
            ["pages.release"] = NavRelease,
            ["pages.viewings"] = NavViewings,
            ["pages.cremations"] = NavCremations,
            ["pages.billing"] = NavBilling,
            ["pages.incomes"] = NavIncomes,
            ["pages.expenses"] = NavExpenses,
            ["pages.revenue"] = NavRevenue,
            ["pages.contacts"] = NavContacts,
            ["pages.certificates"] = NavCertificates,
            ["pages.temperature"] = NavTemperature,
            ["pages.inventory"] = NavInventory,
            ["pages.reports"] = NavReports,
            ["pages.audit"] = NavAudit,
            ["pages.hr"] = NavHumanResource
        };

        foreach (var kvp in visibility)
            kvp.Value.Visibility = PermissionService.Has(kvp.Key)
                ? Visibility.Visible : Visibility.Collapsed;

        NavSettingsBtn.Visibility = PermissionService.Has("pages.settings")
            ? Visibility.Visible : Visibility.Collapsed;

        // Navigate to first permitted page if current page is hidden
        var activeBtn = NavPanel.Children.OfType<Button>().FirstOrDefault(b => !b.IsEnabled);
        if (activeBtn != null && activeBtn.Visibility != Visibility.Visible)
        {
            var first = visibility.Values.FirstOrDefault(b => b.Visibility == Visibility.Visible);
            if (first != null)
                first.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }

    private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        ThemeIcon.Kind = App.IsDarkTheme
            ? MaterialDesignThemes.Wpf.PackIconKind.WeatherNight
            : MaterialDesignThemes.Wpf.PackIconKind.WeatherSunny;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeIcon.Kind = WindowState == WindowState.Maximized
            ? MaterialDesignThemes.Wpf.PackIconKind.WindowRestore
            : MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
    }
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionService.Has("pages.settings"))
        {
            ToastHelper.ShowWarning("You do not have permission to access Settings.");
            return;
        }
        NavigateTo("Settings");
    }

    public async Task RefreshOrgNameAsync()
    {
        try
        {
            using var db = new MortuaryDbContext();
            var appName = await db.Settings.FirstOrDefaultAsync(s => s.Key == "AppName");
            TxtBrandName.Text = !string.IsNullOrWhiteSpace(appName?.Value) ? appName.Value : "Msoft Mortuary Pro";

            var logoPath = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "OrgLogoPath"))?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            {
                ImgSidebarLogo.Source = new BitmapImage(new Uri(Path.GetFullPath(logoPath)));
                ImgSidebarLogo.Visibility = Visibility.Visible;
                SidebarIconBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImgSidebarLogo.Visibility = Visibility.Collapsed;
                SidebarIconBox.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    public void ShowToast(string message, string icon, Color bg)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ShowToast(message, icon, bg));
            return;
        }
        Controls.ToastNotification? toast = null;
        toast = new Controls.ToastNotification(message, icon, bg, () =>
        {
            Dispatcher.Invoke(() => ToastContainer.Children.Remove(toast));
        });
        ToastContainer.Children.Add(toast);
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "SQLite Database|*.db|All Files|*.*",
            FileName = $"mortuary_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };
        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.Copy(MortuaryApp.Data.MortuaryDbContext.GetDbPath(), saveDialog.FileName, true);
                MessageBox.Show("Backup created successfully.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
