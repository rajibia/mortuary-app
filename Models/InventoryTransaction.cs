namespace MortuaryApp.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public int? PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public InventoryItem Item { get; set; } = null!;
}
