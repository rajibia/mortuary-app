namespace MortuaryApp.Models;

public class ReleaseRecord
{
    public int Id { get; set; }
    public int BodyId { get; set; }
    public string ReleasedTo { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? IdNumber { get; set; }
    public string? Phone { get; set; }
    public int? ApprovedBy { get; set; }
    public int? ReleasedBy { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ChecklistNotes { get; set; }
    public bool PaymentCleared { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public MortuaryBody Body { get; set; } = null!;
}
