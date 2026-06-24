namespace MortuaryApp.Models;

public class StorageFeeCharge
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public int? ChargeId { get; set; }
    public DateTime ChargeDate { get; set; }
    public int DaysStored { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal RateApplied { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
    public Charge? Charge { get; set; }
    public FeeReversalLog? ReversalLog { get; set; }
}
