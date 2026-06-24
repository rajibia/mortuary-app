namespace MortuaryApp.Models;

public class Document
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public int? UploadedBy { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int? VerifiedBy { get; set; }
    public string? VerificationNotes { get; set; }
    public string VerificationStatus { get; set; } = "pending";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
