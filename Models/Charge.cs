namespace MortuaryApp.Models;

public class Charge
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public int? ChargeTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? DueDate { get; set; }
    public DateTime? BillingStartDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
    public ChargeType? ChargeType { get; set; }
    public ICollection<StorageFeeCharge> StorageFeeCharges { get; set; } = new List<StorageFeeCharge>();

    public decimal Balance => Amount - PaidAmount;
}
