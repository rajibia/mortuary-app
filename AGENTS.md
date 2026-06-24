# Summary

## Goal
Complete rebuild of the WPF mortuary desktop app with flat light-themed UI using MaterialDesignThemes 5.x Amber accent, dark navy sidebar, light page backgrounds, white card surfaces, no gradients/shadows, plus user management, billing auto-accumulation, polished interactions, and document export features.

## Constraints & Preferences
- Target framework: `net10.0-windows`
- NuGet: MaterialDesignThemes 5.2.1, CommunityToolkit.Mvvm 8.4.0, LiveChartsCore.SkiaSharpView.WPF 2.0.5, Microsoft.EntityFrameworkCore.Sqlite 10.0.9, BCrypt.Net-Next 4.2.0, PDFsharp 6.1.1
- Design language: dark navy sidebar `#0F172A`, amber accent `#F59E0B`, light page bg `#F1F5F9`/`#F4F5F7`, white cards with `1px #E2E8F0`/`#D1D5DB` borders
- **Zero corner radius** on all elements
- MaterialDesign `BundledTheme BaseTheme="Light"` with `PrimaryColor="Amber"`, `SecondaryColor="Blue"`
- Custom chrome: `WindowStyle="None"` + `WindowChrome`; minimize/maximize/close overlaid on outer Border
- App `ShutdownMode="OnExplicitShutdown"`
- Sidebar: `228px` dark navy
- Toast notifications slide at bottom-right of MainWindow
- All phone fields use `PhoneInput` control with country code dropdown + LostFocus validation
- Country codes stored in `Models/CountryCode.cs`; validation per country (Ghana: 9 digits starting 2-5)
- Org name loads from DB settings table on login/main start; sidebar refreshes on Settings save
- Email validation with green/red border on LostFocus via `EmailHelper.WireValidation()`
- Billing auto-accumulation: `Balance = AmountToBePaid − Deposit + (BillingRate × cycles)` where cycles = floor(elapsed / cycleHours) + 1; daily=24h, weekly=168h, monthly=730h; first charge triggers at billing start time
- PDF exports use PDFsharp 6.1.1 with custom `IFontResolver` for system fonts
- Ghana timezone: `Greenwich Standard Time` (UTC+0)

## Progress
### Done
- ReportsPage date range fix: `to` extended to end-of-day (`AddDays(1).AddTicks(-1)`) so same-day ranges include all records
- ReportsPage column generation: switched from `AutoGenerateColumns` (overridden by ModernDataGrid style) to programmatic `DataGridTextColumn` creation from `DataTable.Columns`
- Report card icons: replaced truncated first-letter text with MaterialDesign `PackIcon` (ClipboardTextOutline, CashMultiple, FridgeIndustrialOutline, FlaskOutline, DoorOpen, Certificate, EyeOutline, Fire)
- Global text styles added to CustomStyles.xaml: `ModernPageTitle`, `ModernSectionTitle`, `ModernFormLabel`, `ModernBodyText`, `ModernCaptionText`, `ModernMutedText` (prefix avoids MaterialDesign 5.x key collision)
- Applied consistent font styling across all 17 Views pages using `Style="{StaticResource ...}"` — removed inline `FontSize`/`FontWeight`/`Foreground`
- Export buttons (CSV, PDF, Print) added to ReportsPage detail panel header
- Fixed "calling thread must be STA" error: removed `Task.Run()` wrappers from `ExportToXps()` and `PrintReport()` — both now run directly on UI thread
- Fixed "file in use" error in XPS export: removed redundant `FileStream` — `XpsDocument` handles its own file I/O
- PDFsharp 6.1.1 NuGet added for real PDF generation (replaces XPS)
- `CustomFontResolver.cs` created implementing `IFontResolver` — reads font bytes from Windows Fonts directory via WPF `GlyphTypeface` API
- `GlobalFontSettings.FontResolver = new CustomFontResolver()` registered in `App.xaml.cs:OnStartup`
- PDF export: landscape Letter with org header (name, logo, Ghana time, address, phone, email), data table with auto page break, footer "Powered By Msoft Ghana (www.msoftghana.com)"
- CSV export: comment-header block with org info, Ghana timestamp, "Powered By" line prepended to CSV data
- Print export (FixedDocument): org info block, Ghana time, data table, "Powered By" footer
- GridLinesVisibility changed from `Horizontal` to `All` with `VerticalGridLinesBrush="#E2E8F0"` — all DataGrids now show vertical column separators
- ReleasePage bills panel: when body selected, displays manual charges + auto-accumulated storage balance; release blocked if total outstanding > 0
- Release approval frees cold room: `StorageLocation.Status = "available"` + `Status = "released"`, `StorageLocationId = null`
- Billable Bodies tab: Pay Selected button appears when body selected; user enters amount, system deducts balance up to owed amount, change calculated for overpayment
- ReceiptWindow updated: displays change amount, "Powered By Msoft Ghana" footer, Ghana time, org logo/name/address/phone/email; now uses entity Id after SaveChanges (no fragile LastOrDefault query)
- ReceiptWindow constructor signature: `(int chargeId, decimal amountPaid = 0, decimal changeGiven = 0)` — cleaner API

### In Progress
- (none)

### Blocked
- (none)

## Key Decisions
- **`GridLinesVisibility="All"` instead of "Both"**: WPF enum has `None`, `Horizontal`, `Vertical`, `All` (no "Both")
- **PDFsharp 6.1.1 over XpsDocument**: real PDF output requires external library; XPS is WPF-native but users expect `.pdf` files
- **Custom `IFontResolver` over PlatformFontResolver**: PDFsharp 6.x doesn't bundle fonts or include a platform resolver; reads font bytes from `C:\Windows\Fonts` directory via WPF's `GlyphTypeface.GetFontUri()`
- **Pre-pended CSV comment header**: `# ` lines keep CSV parsable while embedding org details
- **`ComputeBalance()` reused in ReleasePage**: shows the same auto-accumulated balance the Billable Bodies tab computes, giving users consistent billing info before release
- **Text style naming prefix "Modern"**: MaterialDesignThemes 5.x defines `PageTitle` key internally, causing `"Item has already been added"` BAML parse error at startup
- **ReceiptWindow uses entity framework identity**: charge Id is available after `SaveChanges()`, removing need for fragile `OrderByDescending().FirstAsync()` query

## Next Steps
1. Verify all tabbed pages render correctly
2. Port ForgotPasswordWindow and ChangePasswordWindow to flat form styles
3. Add theme toggle persistence (save to settings DB)
4. Remove old/unused code-behind pages
5. Test PDF, CSV, Print export flows end-to-end
6. Verify release flow with bills + cold room deallocation

## Critical Context
- Build command: `dotnet build` — succeeds with 0 errors
- `ShutdownMode="OnExplicitShutdown"` required to prevent app shutdown when LoginWindow closes
- `DatePicker` style setters for `Background`/`BorderBrush`/`BorderThickness` cause BAML parse errors — only set `Height` and `FontSize`
- **Dark mode**: uses `DynamicResource` for 6 themeable brush keys; `ToggleTheme()` replaces brush instances in CustomStyles dictionary
- **All XAML files in Views/ regenerated** after corruption by PowerShell `@()` array flattening bug — files now use proper UTF-8 encoding via `[System.Text.UTF8Encoding]::new($false)`
- **Phone validation per country** in `PhoneInput.xaml.cs` `IsPhoneValid()`: Ghana 9 digits `^[2-5]\d{8}$`, Nigeria 7-11, US 10, UK 10-11, others 5-15
- **Billing computation**: `cycles = (int)Math.Floor(elapsed.TotalHours / cycleHours) + 1` — adds one cycle at start time; cycleHours: daily=24, weekly=168, monthly=730
- **ModernTabControl** uses flat design with amber underline on selected tab, no borders/background
- **Existing `mortuary.db`** won't have `AmountToBePaid` column — `MigrateSchema()` adds it via raw SQL
- **Export threads**: PDF and Print must run on STA UI thread (`Dispatcher.Invoke` not needed when called directly from button handler)
- **GridLinesVisibility = "All"** — the WPF enum value is `All`, not `Both`
- **Global text style naming conflict avoidance**: MaterialDesignThemes 5.x defines internal keys like `PageTitle`, `SectionTitle` — prefixing with `Modern` avoids collisions
- **Ghana time**: `TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time")` — UTC+0, no DST
- **ReleasePage balance check**: considers both `Charges` DB records AND `ComputeBalance()` auto-accumulated storage fees before allowing release

## Relevant Files
- `C:\Users\coki\Desktop\MortuaryApp\Models\MortuaryBody.cs` — `AmountToBePaid` + `StorageLocationId` + billing properties
- `C:\Users\coki\Desktop\MortuaryApp\Models\CountryCode.cs` — 35 countries with dial code + flag emoji
- `C:\Users\coki\Desktop\MortuaryApp\Models\StorageLocation.cs` — `Status` property ("available"/"occupied")
- `C:\Users\coki\Desktop\MortuaryApp\Models\Charge.cs` — `Amount`, `PaidAmount`, `Balance` computed
- `C:\Users\coki\Desktop\MortuaryApp\Controls\PhoneInput.xaml` + `.xaml.cs` — reusable phone input control
- `C:\Users\coki\Desktop\MortuaryApp\Helpers\EmailHelper.cs` — `IsValidEmail()`, `WireValidation()`
- `C:\Users\coki\Desktop\MortuaryApp\Data\DbInitializer.cs` — `EnsureCreated()`, seed superadmin, `MigrateSchema()`
- `C:\Users\coki\Desktop\MortuaryApp\Views\ReportsPage.xaml` + `.xaml.cs` — CSV/PDF/Print exports, programmatic columns, MaterialDesign card icons
- `C:\Users\coki\Desktop\MortuaryApp\Views\ReleasePage.xaml` + `.xaml.cs` — bills panel, `ComputeBalance()` check, storage deallocation on approval
- `C:\Users\coki\Desktop\MortuaryApp\Views\BillingPage.xaml` + `.xaml.cs` — Pay Selected button on Billable Bodies tab, change calculation, receipt display with entity Id
- `C:\Users\coki\Desktop\MortuaryApp\Views\ReceiptWindow.xaml` + `.xaml.cs` — updated with change field, Powered By footer, Ghana time, logo
- `C:\Users\coki\Desktop\MortuaryApp\Views\BodyLocatorPage.xaml` — full-width DataGrid + details panel below
- `C:\Users\coki\Desktop\MortuaryApp\Styles\CustomStyles.xaml` — flat styles, ModernTabControl, ModernComboBox default template, text styles (`Modern*`), GridLinesVisibility="All"
- `C:\Users\coki\Desktop\MortuaryApp\Helpers\CustomFontResolver.cs` — PDFsharp `IFontResolver` reading from Windows Fonts via `GlyphTypeface`
- `C:\Users\coki\Desktop\MortuaryApp\App.xaml.cs` — `GlobalFontSettings.FontResolver = new CustomFontResolver()` in `OnStartup`
- `C:\Users\coki\Desktop\MortuaryApp\MortuaryApp.csproj` — added `PDFsharp 6.1.1` package reference
