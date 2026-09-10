using System.Security.Claims;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace ViteKlub.Web.Authentication;

public sealed record DemoNavigationItem(
    string Href,
    string Label,
    string Icon,
    string Roles,
    NavLinkMatch Match = NavLinkMatch.Prefix,
    bool StartsAdministrationSection = false)
{
    public bool IsAuthorized(ClaimsPrincipal user) =>
        Roles.Split(',').Any(user.IsInRole);
}

public static class DemoNavigation
{
    public static IReadOnlyList<DemoNavigationItem> Items { get; } =
    [
        new("", "Dashboard", Icons.Material.Filled.Dashboard, DemoRoles.All, NavLinkMatch.All),
        new("members", "Iscritti", Icons.Material.Filled.People, DemoRoles.Operational),
        new("memberships", "Abbonamenti", Icons.Material.Filled.CardMembership, DemoRoles.Operational),
        new("accesses", "Accessi", Icons.Material.Filled.Login, DemoRoles.Operational),
        new("payments", "Pagamenti", Icons.Material.Filled.Payments, DemoRoles.Payments),
        new("demo-users", "Utenti demo", Icons.Material.Filled.ManageAccounts, DemoRoles.Administration,
            StartsAdministrationSection: true),
        new("demo", "Gestione demo", Icons.Material.Filled.Science, DemoRoles.Administration),
    ];

    public static bool IsAuthorized(string relativePath, ClaimsPrincipal user)
    {
        string normalizedPath = relativePath.Trim('/');
        DemoNavigationItem? item = Items.SingleOrDefault(item => item.Href == normalizedPath);
        return item?.IsAuthorized(user) ?? false;
    }
}
