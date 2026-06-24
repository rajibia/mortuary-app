using MortuaryApp.Data;
using MortuaryApp.Models;

namespace MortuaryApp.Services;

public class TimelineService
{
    public void AddEvent(int bodyId, string eventName, string? description = null,
        string severity = "info", string? metadata = null)
    {
        using var db = new MortuaryDbContext();
        db.BodyTimelines.Add(new BodyTimeline
        {
            BodyId = bodyId,
            Event = eventName,
            Description = description,
            Severity = severity,
            Metadata = metadata,
            OccurredAt = DateTime.Now
        });
        db.SaveChanges();
    }
}
