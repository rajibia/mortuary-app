namespace MortuaryApp.Models;

public class Income
{
    public int Id { get; set; }
    public int IncomeHead { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string? InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string CurrencySymbol { get; set; } = "GHS";
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
