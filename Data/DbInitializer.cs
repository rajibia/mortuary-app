using System.Linq;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Helpers;
using MortuaryApp.Models;

namespace MortuaryApp.Data;

public static class DbInitializer
{
    public static void Initialize()
    {
        using var db = new MortuaryDbContext();
        db.Database.EnsureCreated();

        MigrateSchema(db);
        SeedRoles(db);

        if (!db.Users.Any())
        {
            var superadmin = new User
            {
                FullName = "Super Administrator",
                Username = "admin",
                Email = "msoftghana@gmail.com",
                PasswordHash = SecurityHelper.HashPassword("mypass"),
                Role = "superadmin",
                CanLogin = true,
                IsActive = true,
                MustChangePassword = false
            };
            db.Users.Add(superadmin);
            db.SaveChanges();
        }
    }

    private static void SeedRoles(MortuaryDbContext db)
    {
        if (db.Roles.Any()) return;

        var allPages = new[]
        {
            "pages.dashboard", "pages.admission", "pages.bodies", "pages.coldroom",
            "pages.embalming", "pages.release", "pages.viewings", "pages.cremations",
            "pages.billing", "pages.incomes", "pages.expenses", "pages.revenue",
            "pages.contacts", "pages.certificates", "pages.temperature",
            "pages.inventory", "pages.reports", "pages.audit", "pages.settings", "pages.hr"
        };

        var allActions = new[]
        {
            "actions.billing.pay", "actions.release.approve", "actions.settings.edit",
            "actions.users.manage", "actions.bodies.delete", "actions.reports.export"
        };

        var allPerms = allPages.Concat(allActions).ToArray();

        db.Roles.AddRange(
            new Role
            {
                Name = "superadmin",
                Description = "Full system access with all permissions",
                Permissions = System.Text.Json.JsonSerializer.Serialize(allPerms),
                IsSystem = true
            },
            new Role
            {
                Name = "admin",
                Description = "Administrative access to all pages and most actions",
                Permissions = System.Text.Json.JsonSerializer.Serialize(allPages.Concat(new[]
                {
                    "actions.billing.pay", "actions.release.approve", "actions.settings.edit",
                    "actions.users.manage", "actions.reports.export"
                })),
                IsSystem = true
            },
            new Role
            {
                Name = "receptionist",
                Description = "Front desk operations — admissions, body locator, billing",
                Permissions = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    "pages.dashboard", "pages.admission", "pages.bodies", "pages.coldroom",
                    "pages.release", "pages.viewings", "pages.contacts", "pages.certificates",
                    "pages.temperature", "pages.billing", "pages.inventory", "pages.reports",
                    "actions.billing.pay", "actions.reports.export"
                }),
                IsSystem = true
            },
            new Role
            {
                Name = "viewer",
                Description = "Read-only access to view records",
                Permissions = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    "pages.dashboard", "pages.bodies", "pages.coldroom",
                    "pages.temperature", "pages.reports"
                }),
                IsSystem = true
            }
        );

        db.SaveChanges();
    }

    private static void MigrateSchema(MortuaryDbContext db)
    {
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Bodies ADD COLUMN AmountToBePaid REAL NOT NULL DEFAULT 0;"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Bodies ADD COLUMN DepositorName TEXT;"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Bodies ADD COLUMN DepositorPhone TEXT;"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Bodies ADD COLUMN DepositorRelationship TEXT;"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Bodies ADD COLUMN DepositorAddress TEXT;"); } catch { }
        try { db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Roles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, Description TEXT NOT NULL DEFAULT '', Permissions TEXT NOT NULL DEFAULT '[]', IsSystem INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL DEFAULT '', UpdatedAt TEXT);"); } catch { }

        BackfillIncome(db);
    }

    private static void BackfillIncome(MortuaryDbContext db)
    {
        try
        {
            var existingIds = db.Incomes.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.StartsWith("CHARGE-"))
                .Select(i => i.InvoiceNumber).ToHashSet();

            var charges = db.Charges.Where(c => c.PaidAmount > 0).ToList();
            foreach (var ch in charges)
            {
                var key = $"CHARGE-{ch.Id}";
                if (existingIds.Contains(key)) continue;

                var body = db.Bodies.Find(ch.BodyId);
                db.Incomes.Add(new Income
                {
                    IncomeHead = 0,
                    Name = $"Payment - {(body?.MortuaryNumber ?? $"Body #{ch.BodyId}")}",
                    Date = ch.PaidAt ?? ch.CreatedAt,
                    Amount = ch.PaidAmount,
                    InvoiceNumber = key,
                    Description = ch.Description,
                    CreatedAt = DateTime.Now
                });
            }
            if (charges.Any(c => !existingIds.Contains($"CHARGE-{c.Id}")))
                db.SaveChanges();
        }
        catch { }
    }
}
