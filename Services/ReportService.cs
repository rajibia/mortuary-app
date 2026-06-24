using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;

namespace MortuaryApp.Services;

public class ReportService
{
    private readonly MortuaryDbContext _db = new();

    public DataTable GetAdmissionReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Mortuary #", typeof(string));
        dt.Columns.Add("Deceased Name", typeof(string));
        dt.Columns.Add("Date of Death", typeof(DateTime));
        dt.Columns.Add("Cause of Death", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Source", typeof(string));
        dt.Columns.Add("Admitted Date", typeof(DateTime));

        var bodies = _db.Bodies.Where(b => b.CreatedAt >= from && b.CreatedAt <= to).ToList();
        foreach (var b in bodies)
            dt.Rows.Add(b.MortuaryNumber, b.DeceasedName, b.DateOfDeath, b.CauseOfDeath,
                b.StatusDisplay, b.Source, b.CreatedAt);
        return dt;
    }

    public DataTable GetFinancialReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Description", typeof(string));
        dt.Columns.Add("Amount", typeof(decimal));
        dt.Columns.Add("Paid", typeof(decimal));
        dt.Columns.Add("Balance", typeof(decimal));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Date", typeof(DateTime));

        var charges = _db.Charges.Include(c => c.Body).Where(c => c.CreatedAt >= from && c.CreatedAt <= to).ToList();
        foreach (var c in charges)
            dt.Rows.Add(c.Body?.DeceasedName, c.Description, c.Amount, c.PaidAmount, c.Balance, c.Status, c.CreatedAt);
        return dt;
    }

    public DataTable GetStorageReport()
    {
        var dt = new DataTable();
        dt.Columns.Add("Room", typeof(string));
        dt.Columns.Add("Bed", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Occupant", typeof(string));
        dt.Columns.Add("Days Occupied", typeof(int));

        var locations = _db.StorageLocations.Include(s => s.Bodies).ToList();
        foreach (var l in locations)
        {
            var occupant = l.Bodies.FirstOrDefault(b => b.Status != "released");
            dt.Rows.Add(l.Room, l.Bed, l.StatusDisplay,
                occupant?.DeceasedName ?? "-",
                occupant != null ? (DateTime.Now - occupant.CreatedAt).Days : 0);
        }
        return dt;
    }

    public DataTable GetEmbalmingReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Started", typeof(DateTime));
        dt.Columns.Add("Completed", typeof(DateTime));
        dt.Columns.Add("Notes", typeof(string));

        var records = _db.EmbalmingRecords.Include(r => r.Body)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to).ToList();
        foreach (var r in records)
            dt.Rows.Add(r.Body?.DeceasedName, r.Status, r.StartedAt, r.CompletedAt, r.Notes);
        return dt;
    }

    public DataTable GetReleaseReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Released To", typeof(string));
        dt.Columns.Add("Relationship", typeof(string));
        dt.Columns.Add("ID Number", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Released At", typeof(DateTime));

        var records = _db.ReleaseRecords.Include(r => r.Body)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to).ToList();
        foreach (var r in records)
            dt.Rows.Add(r.Body?.DeceasedName, r.ReleasedTo, r.Relationship, r.IdNumber, r.Status, r.ReleasedAt);
        return dt;
    }

    public DataTable GetCertificateReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Certificate #", typeof(string));
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Issued To", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Issued At", typeof(DateTime));

        var certs = _db.DeathCertificates.Include(c => c.Body)
            .Where(c => c.CreatedAt >= from && c.CreatedAt <= to).ToList();
        foreach (var c in certs)
            dt.Rows.Add(c.CertificateNumber, c.Body?.DeceasedName, c.IssuedTo, c.Status, c.IssuedAt);
        return dt;
    }

    public DataTable GetViewingReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Requested By", typeof(string));
        dt.Columns.Add("Scheduled", typeof(DateTime));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Notes", typeof(string));

        var viewings = _db.BodyViewings.Include(v => v.Body)
            .Where(v => v.CreatedAt >= from && v.CreatedAt <= to).ToList();
        foreach (var v in viewings)
            dt.Rows.Add(v.Body?.DeceasedName, v.RequestedBy, v.ScheduledAt, v.Status, v.Notes);
        return dt;
    }

    public DataTable GetCremationReport(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        dt.Columns.Add("Body", typeof(string));
        dt.Columns.Add("Scheduled", typeof(DateTime));
        dt.Columns.Add("Cremated", typeof(DateTime));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Ashes Disposition", typeof(string));

        var cremations = _db.Cremations.Include(c => c.Body)
            .Where(c => c.CreatedAt >= from && c.CreatedAt <= to).ToList();
        foreach (var c in cremations)
            dt.Rows.Add(c.Body?.DeceasedName, c.ScheduledAt, c.CrematedAt, c.Status, c.AshesDisposition);
        return dt;
    }

    public string ExportToCsv(DataTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"")));
        foreach (DataRow row in table.Rows)
        {
            var values = row.ItemArray.Select(v => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"");
            sb.AppendLine(string.Join(",", values));
        }
        return sb.ToString();
    }
}
