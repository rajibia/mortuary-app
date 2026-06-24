using System.Text.RegularExpressions;

namespace MortuaryApp.Helpers;

public static class PhoneHelper
{
    public static string FormatGhanaPhone(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var cleaned = input.Trim();
        if (cleaned.StartsWith("+233")) return cleaned;
        if (cleaned.StartsWith("233")) return "+" + cleaned;
        if (cleaned.StartsWith("0")) return "+233" + cleaned[1..];
        return "+233" + cleaned;
    }

    public static bool IsValidGhanaPhone(string phone)
    {
        return !string.IsNullOrWhiteSpace(phone) && Regex.IsMatch(phone, @"^\+233\d{9}$");
    }
}
