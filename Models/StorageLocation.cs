namespace MortuaryApp.Models;

public class StorageLocation
{
    public int Id { get; set; }
    public string Room { get; set; } = string.Empty;
    public string? Chamber { get; set; }
    public string? Rack { get; set; }
    public string Bed { get; set; } = string.Empty;
    public string Status { get; set; } = "available";
    public int Capacity { get; set; } = 1;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MortuaryBody> Bodies { get; set; } = new List<MortuaryBody>();
    public ICollection<TemperatureLog> TemperatureLogs { get; set; } = new List<TemperatureLog>();

    public string DisplayName => $"{Room} / {Chamber ?? "-"} / {Rack ?? "-"} / {Bed}";
    public string StatusDisplay => char.ToUpper(Status[0]) + Status[1..];
}
