namespace MortuaryApp.Models;

public class IdSetting
{
    public int Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Prefix { get; set; } = "MOR";
    public int Digits { get; set; } = 4;
    public int CurrentCounter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
