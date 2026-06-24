namespace MortuaryApp.Models;

public class BodyMovement
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public string? Reason { get; set; }
    public int? MovedBy { get; set; }
    public DateTime MovedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
    public StorageLocation? FromLocation { get; set; }
    public StorageLocation? ToLocation { get; set; }
}
