namespace MortuaryApp.Models;

public class TemperatureLog
{
    public int Id { get; set; }
    public int StorageLocationId { get; set; }
    public decimal Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.Now;
    public int? RecordedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public StorageLocation StorageLocation { get; set; } = null!;
}
