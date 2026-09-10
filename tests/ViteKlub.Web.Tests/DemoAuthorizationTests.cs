using System.Security.Claims;
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
    public void UnknownRouteIsNotAuthorized()
    {
        Assert.False(DemoNavigation.IsAuthorized("not-a-route", CreatePrincipal(DemoRoles.Administrator)));
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
