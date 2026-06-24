using MortuaryApp.Data;
using MortuaryApp.Models;

namespace MortuaryApp.Services;

public class AuditService
{
    public void Log(string module, string activity, string? description = null,
        string severity = "medium", string? metadata = null)
    {
        using var db = new MortuaryDbContext();
        db.AuditLogs.Add(new AuditLog
        {
            Module = module,
            Activity = activity,
            Description = description,
            Severity = severity,
            Metadata = metadata,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }
}
