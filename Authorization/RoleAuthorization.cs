using Microsoft.AspNetCore.Authorization;

namespace InfinityCodexWebApp.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Contributor = "Contributor";
    public const string Reader = "Reader";
}

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManager = "RequireManager";
    public const string RequireContributor = "RequireContributor";
    public const string RequireReader = "RequireReader";

    public static void AddRoleBasedPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireAdmin, policy => policy.RequireRole(AppRoles.Admin));
        options.AddPolicy(RequireManager, policy => policy.RequireRole(AppRoles.Manager));
        options.AddPolicy(RequireContributor, policy => policy.RequireRole(AppRoles.Contributor));
        options.AddPolicy(RequireReader, policy => policy.RequireRole(AppRoles.Reader));
    }
}
