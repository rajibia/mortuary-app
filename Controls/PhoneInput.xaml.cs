using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MortuaryApp.Models;

namespace MortuaryApp.Controls;

public partial class PhoneInput : UserControl
{
    public PhoneInput()
    {
        InitializeComponent();
    }

    public string FullNumber
    {
        get
        {
            var code = (CboCountry.SelectedItem as CountryCode)?.DialCode ?? "+233";
            var digits = Regex.Replace(TxtPhone.Text, @"[^\d]", "");
            return code + digits;
        }
    }

    public string Text
    {
        get => TxtPhone.Text;
        set
        {
            if (value.StartsWith("+"))
            {
                var parsedCode = "";
                var parsedDigits = value;
                foreach (CountryCode c in CboCountry.Items)
                {
                    if (value.StartsWith(c.DialCode) && c.DialCode.Length > parsedCode.Length)
                    {
                        parsedCode = c.DialCode;
                        parsedDigits = value[c.DialCode.Length..];
                    }
                }
                if (!string.IsNullOrEmpty(parsedCode))
                {
                    foreach (CountryCode c in CboCountry.Items)
                    {
                        if (c.DialCode == parsedCode) { CboCountry.SelectedItem = c; break; }
                    }
                }
                TxtPhone.Text = Regex.Replace(parsedDigits, @"[^\d]", "");
            }
            else
            {
                TxtPhone.Text = value;
            }
        }
    }

    public string DialCode
    {
        get => (CboCountry.SelectedItem as CountryCode)?.DialCode ?? "+233";
        set
        {
            foreach (CountryCode c in CboCountry.Items)
            {
                if (c.DialCode == value) { CboCountry.SelectedItem = c; break; }
            }
        }
    }

    public void Clear()
    {
        TxtPhone.Clear();
        TxtValidation.Visibility = Visibility.Collapsed;
    }

    private void TxtPhone_LostFocus(object sender, RoutedEventArgs e)
    {
        var raw = TxtPhone.Text;
        var phone = Regex.Replace(raw, @"[^\d]", "");
        var code = (CboCountry.SelectedItem as CountryCode)?.DialCode ?? "+233";

        if (string.IsNullOrWhiteSpace(raw))
        {
            TxtValidation.Visibility = Visibility.Collapsed;
            return;
        }

        TxtPhone.Text = phone;

        var valid = IsPhoneValid(code, phone);
        TxtValidation.Visibility = Visibility.Visible;
        TxtValidation.Text = valid ? "\u2713" : "\u2717";
        TxtValidation.Foreground = valid
            ? new SolidColorBrush(Color.FromRgb(5, 150, 105))
            : new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }

    private static bool IsPhoneValid(string dialCode, string digits)
    {
        if (string.IsNullOrEmpty(digits)) return false;

        return dialCode switch
        {
            "+233" => digits.Length == 9 && Regex.IsMatch(digits, @"^[2-5]\d{8}$"),
            "+234" => digits.Length >= 7 && digits.Length <= 11,
            "+1" => digits.Length == 10,
            "+44" => digits.Length >= 10 && digits.Length <= 11,
            _ => digits.Length >= 5 && digits.Length <= 15,
        };
    }
}
