namespace MortuaryApp.Models;

public class EmbalmingRecord
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public int? StaffId { get; set; }
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
