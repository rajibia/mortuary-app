namespace MortuaryApp.Models;

public class BodyViewing
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Relationship { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "scheduled";
    public int? ApprovedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
