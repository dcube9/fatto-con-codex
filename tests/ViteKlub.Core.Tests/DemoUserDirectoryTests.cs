using ViteKlub.Core.Data;
using ViteKlub.Core.Users;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class DemoUserDirectoryTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void QueryReturnsUsersInDeterministicOrderWithoutChangingSource()
    {
        DemoUser[] source = Users();

        DemoUserDirectoryResult result = DemoUserDirectory.Query(source, new());

        Assert.Equal(["admin.demo", "manager.demo", "viewer.demo"], result.Items.Select(user => user.Username));
        Assert.Equal(["viewer.demo", "admin.demo", "manager.demo"], source.Select(user => user.Username));
    }

    [Theory]
    [InlineData("ALESSANDRA", "admin.demo")]
    [InlineData("ADMIN.DEMO", "admin.demo")]
    [InlineData("  marco   manager ", "manager.demo")]
    public void SearchIsCaseInsensitiveAcrossTextFields(string search, string expectedUsername)
    {
        DemoUser result = Assert.Single(DemoUserDirectory.Query(Users(), new(Search: search)).Items);

        Assert.Equal(expectedUsername, result.Username);
    }

    [Fact]
    public void RoleFilterReturnsOnlyMatchingUsers()
    {
        DemoUser result = Assert.Single(DemoUserDirectory.Query(Users(), new(Role: DemoRole.Viewer)).Items);

        Assert.Equal("viewer.demo", result.Username);
    }

    [Fact]
    public void SearchAndRoleFilterCanBeCombined()
    {
        Assert.Single(DemoUserDirectory.Query(Users(), new("manager", DemoRole.Manager)).Items);
        Assert.Empty(DemoUserDirectory.Query(Users(), new("manager", DemoRole.Viewer)).Items);
    }

    [Fact]
    public void EmptyDatasetNoMatchAndUnknownRoleReturnEmptyResults()
    {
        Assert.Empty(DemoUserDirectory.Query([], new()).Items);
        Assert.Empty(DemoUserDirectory.Query(Users(), new(Search: "inesistente")).Items);
        Assert.Empty(DemoUserDirectory.Query(Users(), new(Role: (DemoRole)999)).Items);
    }

    [Fact]
    public void EmptyOrNullTextValuesAreHandled()
    {
        DemoUser user = User(null!, string.Empty, DemoRole.Viewer, Guid.Empty);

        Assert.Single(DemoUserDirectory.Query([user], new()).Items);
        Assert.Empty(DemoUserDirectory.Query([user], new(Search: "testo")).Items);
    }

    [Fact]
    public void EqualTextValuesUseIdAsDeterministicTieBreaker()
    {
        DemoUser later = User("same", "Same", DemoRole.Viewer, Guid.Parse("00000000-0000-0000-0000-000000000002"));
        DemoUser earlier = User("same", "Same", DemoRole.Viewer, Guid.Parse("00000000-0000-0000-0000-000000000001"));

        Assert.Equal([earlier.Id, later.Id], DemoUserDirectory.Query([later, earlier], new()).Items.Select(user => user.Id));
    }

    [Fact]
    public void FindsExistingIdAndReturnsNullForUnknownId()
    {
        DemoUser[] users = Users();

        Assert.Same(users[1], DemoUserDirectory.FindById(users, users[1].Id));
        Assert.Null(DemoUserDirectory.FindById(users, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")));
    }

    private static DemoUser[] Users() =>
    [
        User("viewer.demo", "Valerio Viewer", DemoRole.Viewer, Guid.Parse("00000000-0000-0000-0000-000000000003")),
        User("admin.demo", "Alessandra Admin", DemoRole.Administrator, Guid.Parse("00000000-0000-0000-0000-000000000001")),
        User("manager.demo", "Marco Manager", DemoRole.Manager, Guid.Parse("00000000-0000-0000-0000-000000000002")),
    ];

    private static DemoUser User(string username, string displayName, DemoRole role, Guid id) => new()
    {
        Id = id,
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        Username = username,
        DisplayName = displayName,
        Role = role,
        IsActive = true
    };
}
