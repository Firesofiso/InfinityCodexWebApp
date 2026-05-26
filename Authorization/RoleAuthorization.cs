using Microsoft.AspNetCore.Authorization;

namespace InfinityCodexWebApp.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Officer = "Officer";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        Officer,
        Member
    ];
}

public static class AppClaimTypes
{
    public const string UserId = "infinity:user_id";
    public const string Permission = "infinity:permission";
    public const string RegistrationStatus = "infinity:registration_status";
    public const string ImpersonatorUserId = "infinity:impersonator_user_id";
    public const string ImpersonatorDiscordId = "infinity:impersonator_discord_id";
    public const string ImpersonatorDisplayName = "infinity:impersonator_display_name";
}

public static class AppPermissions
{
    public const string ManageDkp = "dkp.manage";
    public const string ManagePlayers = "players.manage";
    public const string ManageUsers = "users.manage";
    public const string ManageRoles = "roles.manage";

    public static readonly IReadOnlyList<string> All =
    [
        ManageDkp,
        ManagePlayers,
        ManageUsers,
        ManageRoles
    ];
}

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, string[]> PermissionByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.Admin] = [AppPermissions.ManageDkp, AppPermissions.ManagePlayers, AppPermissions.ManageUsers, AppPermissions.ManageRoles],
            [AppRoles.Officer] = [AppPermissions.ManageDkp, AppPermissions.ManagePlayers],
            [AppRoles.Member] = []
        };

    public static bool IsValidRole(string? role)
    {
        return !string.IsNullOrWhiteSpace(role)
            && PermissionByRole.ContainsKey(role);
    }

    public static IReadOnlyList<string> GetPermissionsForRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return Array.Empty<string>();
        }

        return PermissionByRole.TryGetValue(role, out string[]? permissions)
            ? permissions
            : Array.Empty<string>();
    }

    public static bool RoleHasPermission(string? role, string permission)
    {
        return GetPermissionsForRole(role).Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireOfficer = "RequireOfficer";
    public const string RequireOfficerOrAdmin = "RequireOfficerOrAdmin";

    public static void AddRoleBasedPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireAdmin, policy => policy.RequireRole(AppRoles.Admin));
        options.AddPolicy(RequireOfficer, policy => policy.RequireRole(AppRoles.Officer));
        options.AddPolicy(RequireOfficerOrAdmin, policy => policy.RequireRole(AppRoles.Admin, AppRoles.Officer));
    }
}
