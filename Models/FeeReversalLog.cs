namespace MortuaryApp.Models;

public class FeeReversalLog
{
    public int Id { get; set; }
    public int StorageFeeChargeId { get; set; }
    public int? ReversedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReversedAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public StorageFeeCharge StorageFeeCharge { get; set; } = null!;
}
