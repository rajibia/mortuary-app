namespace MortuaryApp.Models;

public class ChainOfCustodyLog
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public int? CustodianId { get; set; }
    public int? PreviousCustodianId { get; set; }
    public int? LocationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Purpose { get; set; }
    public string? Signature { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
    public StorageLocation? Location { get; set; }
}
