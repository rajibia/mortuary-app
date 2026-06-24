using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MortuaryApp.Helpers;

public static class EmailHelper
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    public static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    public static void WireValidation(TextBox tb)
    {
        tb.LostFocus += (_, _) =>
        {
            tb.Dispatcher.BeginInvoke(new Action(() =>
            {
                var text = tb.Text.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    tb.ClearValue(TextBox.BorderBrushProperty);
                    tb.ToolTip = null;
                }
                else if (IsValidEmail(text))
                {
                    tb.BorderBrush = new SolidColorBrush(Colors.Green);
                    tb.ToolTip = "Valid email";
                }
                else
                {
                    tb.BorderBrush = new SolidColorBrush(Colors.Red);
                    tb.ToolTip = "Invalid email";
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        };
    }
}
