namespace MortuaryApp.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = "piece";
    public DateTime? ExpiryDate { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierContact { get; set; }
    public string? StorageLocation { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public bool IsLowStock => Quantity <= ReorderLevel;
}
