using System.Collections.Generic;

namespace MortuaryApp.Models;

public class CountryCode
{
    public string Name { get; set; } = "";
    public string DialCode { get; set; } = "";
    public string Flag { get; set; } = "";
    public string DisplayText => $"{Flag}  {Name}  ({DialCode})";

    public static List<CountryCode> All { get; } = new()
    {
        new() { Name = "Ghana", DialCode = "+233", Flag = "\U0001F1EC\U0001F1ED" },
        new() { Name = "Nigeria", DialCode = "+234", Flag = "\U0001F1F3\U0001F1EC" },
        new() { Name = "Kenya", DialCode = "+254", Flag = "\U0001F1F0\U0001F1EA" },
        new() { Name = "South Africa", DialCode = "+27", Flag = "\U0001F1FF\U0001F1E6" },
        new() { Name = "Tanzania", DialCode = "+255", Flag = "\U0001F1F9\U0001F1FF" },
        new() { Name = "Uganda", DialCode = "+256", Flag = "\U0001F1FA\U0001F1EC" },
        new() { Name = "Ethiopia", DialCode = "+251", Flag = "\U0001F1EA\U0001F1F9" },
        new() { Name = "Egypt", DialCode = "+20", Flag = "\U0001F1EA\U0001F1EC" },
        new() { Name = "Morocco", DialCode = "+212", Flag = "\U0001F1F2\U0001F1E6" },
        new() { Name = "Senegal", DialCode = "+221", Flag = "\U0001F1F8\U0001F1F3" },
        new() { Name = "Côte d'Ivoire", DialCode = "+225", Flag = "\U0001F1E8\U0001F1EE" },
        new() { Name = "Cameroon", DialCode = "+237", Flag = "\U0001F1E8\U0001F1F2" },
        new() { Name = "Rwanda", DialCode = "+250", Flag = "\U0001F1F7\U0001F1FC" },
        new() { Name = "United States", DialCode = "+1", Flag = "\U0001F1FA\U0001F1F8" },
        new() { Name = "Canada", DialCode = "+1", Flag = "\U0001F1E8\U0001F1E6" },
        new() { Name = "United Kingdom", DialCode = "+44", Flag = "\U0001F1EC\U0001F1E7" },
        new() { Name = "Germany", DialCode = "+49", Flag = "\U0001F1E9\U0001F1EA" },
        new() { Name = "France", DialCode = "+33", Flag = "\U0001F1EB\U0001F1F7" },
        new() { Name = "Italy", DialCode = "+39", Flag = "\U0001F1EE\U0001F1F9" },
        new() { Name = "Spain", DialCode = "+34", Flag = "\U0001F1EA\U0001F1F8" },
        new() { Name = "Netherlands", DialCode = "+31", Flag = "\U0001F1F3\U0001F1F1" },
        new() { Name = "India", DialCode = "+91", Flag = "\U0001F1EE\U0001F1F3" },
        new() { Name = "China", DialCode = "+86", Flag = "\U0001F1E8\U0001F1F3" },
        new() { Name = "Australia", DialCode = "+61", Flag = "\U0001F1E6\U0001F1FA" },
        new() { Name = "Brazil", DialCode = "+55", Flag = "\U0001F1E7\U0001F1F7" },
        new() { Name = "Japan", DialCode = "+81", Flag = "\U0001F1EF\U0001F1F5" },
        new() { Name = "Togo", DialCode = "+228", Flag = "\U0001F1F9\U0001F1EC" },
        new() { Name = "Benin", DialCode = "+229", Flag = "\U0001F1E7\U0001F1EF" },
        new() { Name = "Burkina Faso", DialCode = "+226", Flag = "\U0001F1E7\U0001F1EB" },
        new() { Name = "Mali", DialCode = "+223", Flag = "\U0001F1F2\U0001F1F1" },
        new() { Name = "Niger", DialCode = "+227", Flag = "\U0001F1F3\U0001F1EA" },
        new() { Name = "Liberia", DialCode = "+231", Flag = "\U0001F1F1\U0001F1F7" },
        new() { Name = "Sierra Leone", DialCode = "+232", Flag = "\U0001F1F8\U0001F1F1" },
        new() { Name = "Gambia", DialCode = "+220", Flag = "\U0001F1EC\U0001F1F2" },
        new() { Name = "Guinea", DialCode = "+224", Flag = "\U0001F1EC\U0001F1F3" },
    };
}
