namespace MortuaryApp.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = "medium";
    public string? IpAddress { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
