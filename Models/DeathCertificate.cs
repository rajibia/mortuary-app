namespace MortuaryApp.Models;

public class DeathCertificate
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string IssuedTo { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? IdType { get; set; }
    public string? IdNumber { get; set; }
    public string? CauseOfDeath { get; set; }
    public string? PlaceOfDeath { get; set; }
    public string Status { get; set; } = "pending";
    public int? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
