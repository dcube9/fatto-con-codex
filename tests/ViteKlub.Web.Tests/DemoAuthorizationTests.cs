using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ViteKlub.Web.Authentication;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class DemoAuthorizationTests
{
    public static TheoryData<string, string[]> AuthorizedMenuItems => new()
    {
        { DemoRoles.Administrator, ["", "members", "memberships", "accesses", "payments", "demo-users", "demo"] },
        { DemoRoles.Manager, ["", "members", "memberships", "accesses", "payments"] },
        { DemoRoles.Receptionist, ["", "members", "memberships", "accesses", "payments"] },
        { DemoRoles.Viewer, ["", "members", "memberships", "accesses"] },
    };

    [Theory]
    [MemberData(nameof(AuthorizedMenuItems))]
    public void NavigationMatchesRoleMatrix(string role, string[] expectedItems)
    {
        ClaimsPrincipal user = CreatePrincipal(role);

        string[] actualItems = DemoNavigation.Items
            .Where(item => item.IsAuthorized(user))
            .Select(item => item.Href)
            .ToArray();

        Assert.Equal(expectedItems, actualItems);
        Assert.All(expectedItems, path => Assert.True(DemoNavigation.IsAuthorized(path, user)));
    }

    [Fact]
    public void AnonymousUserHasNoAuthorizedMenuOrRoutes()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.DoesNotContain(DemoNavigation.Items, item => item.IsAuthorized(anonymous));
        Assert.False(DemoNavigation.IsAuthorized("members", anonymous));
    }

    [Fact]
    public void DashboardRouteRequiresEveryAuthenticatedDemoRole()
    {
        AuthorizeAttribute attribute = Assert.Single(typeof(ViteKlub.Web.Pages.Home)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(DemoRoles.All, attribute.Roles);
        Assert.All([DemoRoles.Administrator, DemoRoles.Manager, DemoRoles.Receptionist, DemoRoles.Viewer],
            role => Assert.Contains(role, attribute.Roles!.Split(',')));
    }

    [Fact]
    public void UnknownRouteIsNotAuthorized()
    {
        Assert.False(DemoNavigation.IsAuthorized("not-a-route", CreatePrincipal(DemoRoles.Administrator)));
    }

    [Theory]
    [InlineData(typeof(ViteKlub.Web.Pages.Payments))]
    [InlineData(typeof(ViteKlub.Web.Pages.PaymentDetail))]
    public void PaymentRoutesRequireThePaymentRoleMatrix(Type pageType)
    {
        AuthorizeAttribute attribute = Assert.Single(pageType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(DemoRoles.Payments, attribute.Roles);
        Assert.All([DemoRoles.Administrator, DemoRoles.Manager, DemoRoles.Receptionist],
            role => Assert.Contains(role, attribute.Roles!.Split(',')));
        Assert.DoesNotContain(DemoRoles.Viewer, attribute.Roles!.Split(','));
    }

    [Theory]
    [InlineData(typeof(ViteKlub.Web.Pages.DemoUsers))]
    [InlineData(typeof(ViteKlub.Web.Pages.DemoUserDetail))]
    public void DemoUserRoutesRequireAdministrator(Type pageType)
    {
        AuthorizeAttribute attribute = Assert.Single(pageType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(DemoRoles.Administration, attribute.Roles);
        Assert.Equal(DemoRoles.Administrator, attribute.Roles);
        Assert.All([DemoRoles.Manager, DemoRoles.Receptionist, DemoRoles.Viewer],
            role => Assert.DoesNotContain(role, attribute.Roles!.Split(',')));
    }

    [Fact]
    public void DemoManagementRouteRequiresAdministrator()
    {
        AuthorizeAttribute attribute = Assert.Single(typeof(ViteKlub.Web.Pages.DemoManagement)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(DemoRoles.Administrator, attribute.Roles);
        Assert.All([DemoRoles.Manager, DemoRoles.Receptionist, DemoRoles.Viewer],
            role => Assert.DoesNotContain(role, attribute.Roles!.Split(',')));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData("/members", "members")]
    [InlineData("/members?active=true", "members?active=true")]
    [InlineData("//example.com", "")]
    [InlineData("/\\example.com", "")]
    [InlineData("https://example.com", "")]
    public void ReturnUrlIsSafeAndRelativeToTheApplicationBase(string? returnUrl, string expected)
    {
        Assert.Equal(expected, DemoNavigation.GetSafeReturnUrl(returnUrl));
    }

    private static ClaimsPrincipal CreatePrincipal(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));
}
