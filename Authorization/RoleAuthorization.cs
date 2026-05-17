using Microsoft.AspNetCore.Authorization;

namespace InfinityCodexWebApp.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Contributor = "Contributor";
    public const string Reader = "Reader";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        Manager,
        Contributor,
        Reader
    ];
}

public static class AppClaimTypes
{
    public const string Permission = "infinity:permission";
}

public static class AppPermissions
{
    public const string ManageDkp = "dkp.manage";
    public const string ManageUsers = "users.manage";
    public const string ManageRoles = "roles.manage";

    public static readonly IReadOnlyList<string> All =
    [
        ManageDkp,
        ManageUsers,
        ManageRoles
    ];
}

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, string[]> PermissionByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.Admin] = [AppPermissions.ManageDkp, AppPermissions.ManageUsers, AppPermissions.ManageRoles],
            [AppRoles.Manager] = [AppPermissions.ManageDkp, AppPermissions.ManageUsers],
            [AppRoles.Contributor] = [],
            [AppRoles.Reader] = []
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
    public const string RequireManager = "RequireManager";
    public const string RequireManagerOrAdmin = "RequireManagerOrAdmin";
    public const string RequireContributor = "RequireContributor";
    public const string RequireReader = "RequireReader";

    public static void AddRoleBasedPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireAdmin, policy => policy.RequireRole(AppRoles.Admin));
        options.AddPolicy(RequireManager, policy => policy.RequireRole(AppRoles.Manager));
        options.AddPolicy(RequireManagerOrAdmin, policy => policy.RequireRole(AppRoles.Admin, AppRoles.Manager));
        options.AddPolicy(RequireContributor, policy => policy.RequireRole(AppRoles.Contributor));
        options.AddPolicy(RequireReader, policy => policy.RequireRole(AppRoles.Reader));
    }
}
