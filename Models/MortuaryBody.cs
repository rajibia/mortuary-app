namespace MortuaryApp.Models;

public class MortuaryBody
{
    public int Id { get; set; }
    public string MortuaryNumber { get; set; } = string.Empty;
    public string DeceasedName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public TimeSpan? TimeOfDeath { get; set; }
    public string? CauseOfDeath { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "admitted";
    public int? NextOfKinId { get; set; }
    public string? NextOfKinName { get; set; }
    public string? DepositorName { get; set; }
    public string? DepositorAddress { get; set; }
    public string? DepositorPhone { get; set; }
    public string? DepositorRelationship { get; set; }
    public string? PhotoPath { get; set; }
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public int? StorageLocationId { get; set; }
    public string? AdmissionNotes { get; set; }
    public DateTime? DueDate { get; set; }
    public string BillingType { get; set; } = "daily";
    public decimal AmountToBePaid { get; set; }
    public decimal BillingRate { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTime? BillingStartAt { get; set; }
    public DateTime? RecycledAt { get; set; }
    public DateTime? UnclaimedAt { get; set; }
    public DateTime? StatutoryNoticeSentAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public NextOfKin? NextOfKin { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public ICollection<BodyMovement> Movements { get; set; } = new List<BodyMovement>();
    public ICollection<EmbalmingRecord> EmbalmingRecords { get; set; } = new List<EmbalmingRecord>();
    public ReleaseRecord? ReleaseRecord { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
    public ICollection<BodyTimeline> Timeline { get; set; } = new List<BodyTimeline>();
    public ICollection<StorageFeeCharge> StorageFeeCharges { get; set; } = new List<StorageFeeCharge>();
    public ICollection<ChainOfCustodyLog> ChainOfCustodyLogs { get; set; } = new List<ChainOfCustodyLog>();
    public DeathCertificate? DeathCertificate { get; set; }
    public ICollection<BodyViewing> Viewings { get; set; } = new List<BodyViewing>();
    public Cremation? Cremation { get; set; }

    public string FullName => DeceasedName;
    public string StatusDisplay => char.ToUpper(Status[0]) + Status[1..].Replace('_', ' ');
}
