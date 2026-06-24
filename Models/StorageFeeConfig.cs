namespace MortuaryApp.Models;

public class StorageFeeConfig
{
    public int Id { get; set; }
    public int GracePeriodHours { get; set; } = 48;
    public decimal StandardRatePerDay { get; set; } = 120.00m;
    public decimal Tier2Multiplier { get; set; } = 1.50m;
    public decimal Tier3Multiplier { get; set; } = 2.00m;
    public int LongStayPenaltyAfterDays { get; set; } = 30;
    public decimal LongStayPenaltyAmount { get; set; } = 500.00m;
    public string Currency { get; set; } = "GHS";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
