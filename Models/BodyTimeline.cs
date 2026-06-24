namespace MortuaryApp.Models;

public class BodyTimeline
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PerformedBy { get; set; }
    public string Severity { get; set; } = "info";
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
