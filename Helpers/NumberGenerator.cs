using MortuaryApp.Data;

namespace MortuaryApp.Helpers;

public static class NumberGenerator
{
    public static string GenerateMortuaryNumber()
    {
        using var db = new MortuaryDbContext();
        var setting = db.IdSettings.FirstOrDefault(s => s.Scope == "mortuary_number");
        if (setting == null)
        {
            setting = new Models.IdSetting
            {
                Scope = "mortuary_number",
                Prefix = "MOR",
                Digits = 4,
                CurrentCounter = 1,
                Enabled = true
            };
            db.IdSettings.Add(setting);
            db.SaveChanges();
            return $"{setting.Prefix}{1:D4}";
        }

        setting.CurrentCounter++;
        db.SaveChanges();
        return $"{setting.Prefix}{setting.CurrentCounter:D4}";
    }

    public static string GenerateCertificateNumber()
    {
        using var db = new MortuaryDbContext();
        var setting = db.IdSettings.FirstOrDefault(s => s.Scope == "certificate_number");
        if (setting == null)
        {
            setting = new Models.IdSetting
            {
                Scope = "certificate_number",
                Prefix = "DC",
                Digits = 5,
                CurrentCounter = 1,
                Enabled = true
            };
            db.IdSettings.Add(setting);
            db.SaveChanges();
            return $"DC-{DateTime.Now.Year}-{1:D5}";
        }

        setting.CurrentCounter++;
        db.SaveChanges();
        return $"DC-{DateTime.Now.Year}-{setting.CurrentCounter:D5}";
    }

    public static string GenerateQrCode()
    {
        return Guid.NewGuid().ToString("N")[..16].ToUpper();
    }
}
