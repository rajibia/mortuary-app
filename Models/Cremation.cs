namespace MortuaryApp.Models;

public class Cremation
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CrematedAt { get; set; }
    public string Status { get; set; } = "scheduled";
    public string? AuthorizedBy { get; set; }
    public string? AuthorizationDocument { get; set; }
    public string? AshesDisposition { get; set; }
    public string? AshesCollectedBy { get; set; }
    public DateTime? AshesCollectedAt { get; set; }
    public int? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
