using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;
using MortuaryApp.Models;

namespace MortuaryApp.Services;

public class StorageFeeService
{
    public void ProcessDailyFees()
    {
        using var db = new MortuaryDbContext();
        var config = db.StorageFeeConfigs.FirstOrDefault(c => c.IsActive);
        if (config == null) return;

        var holidays = db.PublicHolidays.Where(h => h.IsFeeExempt).Select(h => h.Date).ToList();
        var bodies = db.Bodies.Where(b => b.Status == "stored" || b.Status == "admitted")
            .Include(b => b.StorageFeeCharges).ToList();

        foreach (var body in bodies)
        {
            var admissionDate = body.CreatedAt.Date;
            var today = DateTime.Now.Date;
            var daysSince = (today - admissionDate).Days + 1;

            if (daysSince <= config.GracePeriodHours / 24) continue;
            if (holidays.Contains(today)) continue;
            if (body.StorageFeeCharges.Any(f => f.ChargeDate == today)) continue;

            var (tier, rate) = CalculateTier(daysSince, config);
            var fee = new StorageFeeCharge
            {
                BodyId = body.Id,
                ChargeDate = today,
                DaysStored = daysSince,
                Tier = tier,
                RateApplied = rate,
                Amount = rate
            };
            db.StorageFeeCharges.Add(fee);
        }
        db.SaveChanges();
    }

    public (string Tier, decimal Rate) CalculateTier(int daysSinceAdmission, StorageFeeConfig? config = null)
    {
        config ??= new StorageFeeConfig();
        var graceDays = config.GracePeriodHours / 24;
        var adjustedDays = daysSinceAdmission - graceDays;

        if (adjustedDays <= 0) return ("grace", 0);
        if (adjustedDays <= 7) return ("tier1", config.StandardRatePerDay);
        if (adjustedDays <= 14) return ("tier2", config.StandardRatePerDay * config.Tier2Multiplier);
        if (adjustedDays <= config.LongStayPenaltyAfterDays)
            return ("tier3", config.StandardRatePerDay * config.Tier3Multiplier);
        return ("long_stay_penalty", config.LongStayPenaltyAmount);
    }

    public StorageFeeSummary GetFeeSummary(int bodyId)
    {
        using var db = new MortuaryDbContext();
        var body = db.Bodies.Include(b => b.StorageFeeCharges).First(b => b.Id == bodyId);
        var config = db.StorageFeeConfigs.FirstOrDefault(c => c.IsActive) ?? new StorageFeeConfig();

        var charges = body.StorageFeeCharges.ToList();
        var totalCharged = charges.Sum(c => c.Amount);
        var totalReversed = charges.Where(c => c.ReversalLog != null).Sum(c => c.Amount);
        var daysBilled = charges.Count;
        var admissionDate = body.CreatedAt.Date;
        var totalDays = (DateTime.Now.Date - admissionDate).Days + 1;
        var (currentTier, _) = CalculateTier(totalDays, config);

        return new StorageFeeSummary
        {
            TotalCharged = totalCharged,
            TotalReversed = totalReversed,
            NetCharge = totalCharged - totalReversed,
            DaysBilled = daysBilled,
            TotalDays = totalDays,
            CurrentTier = currentTier
        };
    }

    public void ReverseFee(int storageFeeChargeId, string reason)
    {
        using var db = new MortuaryDbContext();
        var fee = db.StorageFeeCharges.Include(f => f.ReversalLog).First(f => f.Id == storageFeeChargeId);
        if (fee.ReversalLog != null) return;

        db.FeeReversalLogs.Add(new FeeReversalLog
        {
            StorageFeeChargeId = fee.Id,
            Reason = reason,
            ReversedAt = DateTime.Now
        });
        db.SaveChanges();
    }
}

public class StorageFeeSummary
{
    public decimal TotalCharged { get; set; }
    public decimal TotalReversed { get; set; }
    public decimal NetCharge { get; set; }
    public int DaysBilled { get; set; }
    public int TotalDays { get; set; }
    public string CurrentTier { get; set; } = string.Empty;
}
