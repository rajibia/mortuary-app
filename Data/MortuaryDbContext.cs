using System.IO;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Models;

namespace MortuaryApp.Data;

public class MortuaryDbContext : DbContext
{
    public DbSet<MortuaryBody> Bodies { get; set; }
    public DbSet<NextOfKin> NextOfKins { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }
    public DbSet<BodyMovement> BodyMovements { get; set; }
    public DbSet<EmbalmingRecord> EmbalmingRecords { get; set; }
    public DbSet<ReleaseRecord> ReleaseRecords { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<ChargeType> ChargeTypes { get; set; }
    public DbSet<Charge> Charges { get; set; }
    public DbSet<StorageFeeConfig> StorageFeeConfigs { get; set; }
    public DbSet<StorageFeeCharge> StorageFeeCharges { get; set; }
    public DbSet<FeeReversalLog> FeeReversalLogs { get; set; }
    public DbSet<PublicHoliday> PublicHolidays { get; set; }
    public DbSet<BodyTimeline> BodyTimelines { get; set; }
    public DbSet<BodyViewing> BodyViewings { get; set; }
    public DbSet<Cremation> Cremations { get; set; }
    public DbSet<DeathCertificate> DeathCertificates { get; set; }
    public DbSet<TemperatureLog> TemperatureLogs { get; set; }
    public DbSet<ChainOfCustodyLog> ChainOfCustodyLogs { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<Income> Incomes { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<IdSetting> IdSettings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    public static string GetDbPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mortuary Pro");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "mortuary.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={GetDbPath()}");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<MortuaryBody>(e =>
        {
            e.HasIndex(b => b.MortuaryNumber).IsUnique();
            e.HasIndex(b => b.Barcode).IsUnique();
            e.HasIndex(b => b.QrCode).IsUnique();
            e.Property(b => b.Status).HasDefaultValue("admitted");
            e.Property(b => b.BillingType).HasDefaultValue("daily");
            e.HasOne(b => b.NextOfKin).WithMany(n => n.Bodies).HasForeignKey(b => b.NextOfKinId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(b => b.StorageLocation).WithMany(s => s.Bodies).HasForeignKey(b => b.StorageLocationId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(b => b.DeletedAt == null);
        });

        model.Entity<NextOfKin>(e =>
        {
            e.HasIndex(n => n.Phone);
        });

        model.Entity<StorageLocation>(e =>
        {
            e.Property(s => s.Status).HasDefaultValue("available");
            e.Property(s => s.Capacity).HasDefaultValue(1);
            e.HasQueryFilter(s => s.DeletedAt == null);
        });

        model.Entity<BodyMovement>(e =>
        {
            e.HasOne(m => m.FromLocation).WithMany().HasForeignKey(m => m.FromLocationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(m => m.ToLocation).WithMany().HasForeignKey(m => m.ToLocationId).OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<EmbalmingRecord>(e =>
        {
            e.Property(r => r.Status).HasDefaultValue("pending");
        });

        model.Entity<ReleaseRecord>(e =>
        {
            e.Property(r => r.Status).HasDefaultValue("pending");
            e.HasOne(r => r.Body).WithOne(b => b.ReleaseRecord).HasForeignKey<ReleaseRecord>(r => r.BodyId);
        });

        model.Entity<Document>(e =>
        {
            e.Property(d => d.VerificationStatus).HasDefaultValue("pending");
        });

        model.Entity<ChargeType>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.IsActive).HasDefaultValue(true);
        });

        model.Entity<Charge>(e =>
        {
            e.Property(c => c.Status).HasDefaultValue("pending");
        });

        model.Entity<StorageFeeConfig>(e =>
        {
            e.Property(c => c.IsActive).HasDefaultValue(true);
        });

        model.Entity<StorageFeeCharge>(e =>
        {
            e.HasIndex(s => new { s.BodyId, s.ChargeDate }).IsUnique();
            e.HasOne(s => s.Charge).WithMany(c => c.StorageFeeCharges).HasForeignKey(s => s.ChargeId).OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<FeeReversalLog>(e =>
        {
            e.HasOne(f => f.StorageFeeCharge).WithOne(s => s.ReversalLog).HasForeignKey<FeeReversalLog>(f => f.StorageFeeChargeId);
        });

        model.Entity<PublicHoliday>(e =>
        {
            e.HasIndex(p => p.Date).IsUnique();
            e.Property(p => p.IsFeeExempt).HasDefaultValue(true);
        });

        model.Entity<BodyViewing>(e =>
        {
            e.Property(v => v.Status).HasDefaultValue("scheduled");
        });

        model.Entity<Cremation>(e =>
        {
            e.Property(c => c.Status).HasDefaultValue("scheduled");
            e.HasOne(c => c.Body).WithOne(b => b.Cremation).HasForeignKey<Cremation>(c => c.BodyId);
        });

        model.Entity<DeathCertificate>(e =>
        {
            e.HasIndex(d => d.CertificateNumber).IsUnique();
            e.Property(d => d.Status).HasDefaultValue("pending");
            e.HasOne(d => d.Body).WithOne(b => b.DeathCertificate).HasForeignKey<DeathCertificate>(d => d.BodyId);
        });

        model.Entity<TemperatureLog>(e =>
        {
            e.HasOne(t => t.StorageLocation).WithMany(s => s.TemperatureLogs).HasForeignKey(t => t.StorageLocationId);
        });

        model.Entity<ChainOfCustodyLog>(e =>
        {
            e.HasOne(c => c.Location).WithMany().HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<InventoryItem>(e =>
        {
            e.HasIndex(i => i.Sku).IsUnique();
            e.Property(i => i.IsActive).HasDefaultValue(true);
            e.HasQueryFilter(i => i.DeletedAt == null);
        });

        model.Entity<InventoryTransaction>(e =>
        {
            e.HasOne(t => t.Item).WithMany(i => i.Transactions).HasForeignKey(t => t.ItemId);
        });

        model.Entity<Income>(e =>
        {
            e.HasQueryFilter(i => i.DeletedAt == null);
        });

        model.Entity<Expense>(e =>
        {
            e.HasQueryFilter(e2 => e2.DeletedAt == null);
        });

        model.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.Module);
            e.HasIndex(a => a.CreatedAt);
        });

        model.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.ReadAt });
        });

        model.Entity<Setting>(e =>
        {
            e.HasIndex(s => s.Key).IsUnique();
        });

        model.Entity<Department>(e =>
        {
            e.Property(d => d.IsActive).HasDefaultValue(true);
        });

        model.Entity<IdSetting>(e =>
        {
            e.HasIndex(i => i.Scope).IsUnique();
            e.Property(i => i.Enabled).HasDefaultValue(true);
            e.Property(i => i.Prefix).HasDefaultValue("MOR");
            e.Property(i => i.Digits).HasDefaultValue(4);
        });

        model.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("receptionist");
            e.Property(u => u.IsActive).HasDefaultValue(true);
            e.Property(u => u.CanLogin).HasDefaultValue(true);
        });

        model.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
        });
    }
}
