using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;

namespace MortuaryApp.Services;

public static class PermissionService
{
    private static HashSet<string>? _cachedPermissions;

    public static async Task LoadAsync()
    {
        var user = App.CurrentUser;
        if (user == null) { _cachedPermissions = null; return; }

        using var db = new MortuaryDbContext();
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == user.Role);
        _cachedPermissions = role != null
            ? JsonSerializer.Deserialize<HashSet<string>>(role.Permissions) ?? new HashSet<string>()
            : new HashSet<string>();
    }

    public static bool Has(string permission)
    {
        if (_cachedPermissions == null) return false;
        if (_cachedPermissions.Contains("*")) return true;
        return _cachedPermissions.Contains(permission);
    }

    public static void Invalidate()
    {
        _cachedPermissions = null;
    }
}
