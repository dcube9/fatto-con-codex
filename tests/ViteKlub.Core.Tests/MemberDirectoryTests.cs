using ViteKlub.Core.Data;
using ViteKlub.Core.Members;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class MemberDirectoryTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void QueryProjectsMembersWithoutChangingSource()
    {
        Member[] source = [Member("002", "Luca", "Verdi"), Member("001", "Anna", "Bianchi")];

        MemberDirectoryResult result = MemberDirectory.Query(source, new());

        Assert.Equal(["001", "002"], result.Items.Select(member => member.MemberNumber));
        Assert.Equal(["002", "001"], source.Select(member => member.MemberNumber));
        Assert.Same(source[1], result.Items[0]);
    }

    [Theory]
    [InlineData("vk-002", "VK-002")]
    [InlineData("ANNA", "VK-001")]
    [InlineData("bianchi", "VK-001")]
    [InlineData("Anna Bianchi", "VK-001")]
    [InlineData("  aNNa   BIANCHI  ", "VK-001")]
    public void SearchMatchesSupportedValuesCaseInsensitively(string search, string expectedNumber)
    {
        MemberDirectoryResult result = MemberDirectory.Query(Members(), new(Search: search));

        Assert.Equal(expectedNumber, Assert.Single(result.Items).MemberNumber);
    }

    [Theory]
    [InlineData(MemberStatus.Active, "VK-001")]
    [InlineData(MemberStatus.Suspended, "VK-002")]
    [InlineData(MemberStatus.Archived, "VK-003")]
    public void StatusFilterReturnsMatchingMember(MemberStatus status, string expectedNumber)
    {
        MemberDirectoryResult result = MemberDirectory.Query(Members(), new(Status: status));

        Assert.Equal(expectedNumber, Assert.Single(result.Items).MemberNumber);
    }

    [Fact]
    public void SearchAndStatusCanBeCombined()
    {
        Assert.Single(MemberDirectory.Query(Members(), new("luca", MemberStatus.Suspended)).Items);
        Assert.Empty(MemberDirectory.Query(Members(), new("luca", MemberStatus.Active)).Items);
    }

    [Fact]
    public void SortsAscendingAndDescending()
    {
        Assert.Equal(
            ["VK-001", "VK-002", "VK-003"],
            MemberDirectory.Query(Members(), new(SortBy: MemberSortField.JoinedOn)).Items.Select(x => x.MemberNumber));
        Assert.Equal(
            ["VK-003", "VK-002", "VK-001"],
            MemberDirectory.Query(Members(), new(SortBy: MemberSortField.JoinedOn, Direction: MemberSortDirection.Descending)).Items.Select(x => x.MemberNumber));
    }

    [Fact]
    public void EqualSortValuesUseIdAsDeterministicTieBreaker()
    {
        Member laterId = Member("002", "A", "A", id: Guid.Parse("00000000-0000-0000-0000-000000000002"));
        Member earlierId = Member("002", "B", "B", id: Guid.Parse("00000000-0000-0000-0000-000000000001"));

        MemberDirectoryResult result = MemberDirectory.Query([laterId, earlierId], new());

        Assert.Equal([earlierId.Id, laterId.Id], result.Items.Select(x => x.Id));
    }

    [Fact]
    public void PaginatesFirstAndSubsequentPages()
    {
        Assert.Equal(["VK-001", "VK-002"], MemberDirectory.Query(Members(), new(PageSize: 2)).Items.Select(x => x.MemberNumber));
        Assert.Equal(["VK-003"], MemberDirectory.Query(Members(), new(Page: 2, PageSize: 2)).Items.Select(x => x.MemberNumber));
    }

    [Fact]
    public void PagePastAvailableResultsIsEmptyButKeepsTotal()
    {
        MemberDirectoryResult result = MemberDirectory.Query(Members(), new(Page: 9, PageSize: 2));

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.PageCount);
    }

    [Fact]
    public void EmptyDatasetAndNoMatchesReturnEmptyResults()
    {
        Assert.Empty(MemberDirectory.Query([], new()).Items);
        Assert.Empty(MemberDirectory.Query(Members(), new(Search: "inesistente")).Items);
    }

    [Theory]
    [InlineData(null, MedicalCertificateState.Missing)]
    [InlineData("2026-08-31", MedicalCertificateState.Expired)]
    [InlineData("2026-09-01", MedicalCertificateState.ExpiringSoon)]
    [InlineData("2026-10-01", MedicalCertificateState.ExpiringSoon)]
    [InlineData("2026-10-02", MedicalCertificateState.Valid)]
    public void ClassifiesCertificateAtFixedBoundaries(string? expiry, MedicalCertificateState expected)
    {
        DateOnly? expiresOn = expiry is null ? null : DateOnly.ParseExact(expiry, "yyyy-MM-dd");

        Assert.Equal(expected, MemberDirectory.ClassifyCertificate(expiresOn, new(2026, 9, 1)));
    }

    [Theory]
    [InlineData(MemberStatus.Active, "Attivo")]
    [InlineData(MemberStatus.Suspended, "Sospeso")]
    [InlineData(MemberStatus.Archived, "Archiviato")]
    public void TranslatesMemberStatusToItalian(MemberStatus status, string expected)
    {
        Assert.Equal(expected, MemberDirectory.MemberStatusLabel(status));
    }

    [Fact]
    public void FindsExistingIdAndReturnsNullForUnknownId()
    {
        Member[] members = Members();
        Member expected = members[1];

        Assert.Same(expected, MemberDirectory.FindById(members, expected.Id));
        Assert.Null(MemberDirectory.FindById(members, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")));
    }

    private static Member[] Members() =>
    [
        Member("VK-003", "Marco", "Rossi", MemberStatus.Archived, new(2026, 3, 1), Guid.Parse("00000000-0000-0000-0000-000000000003")),
        Member("VK-001", "Anna", "Bianchi", MemberStatus.Active, new(2026, 1, 1), Guid.Parse("00000000-0000-0000-0000-000000000001")),
        Member("VK-002", "Luca", "Verdi", MemberStatus.Suspended, new(2026, 2, 1), Guid.Parse("00000000-0000-0000-0000-000000000002")),
    ];

    private static Member Member(
        string number,
        string firstName,
        string lastName,
        MemberStatus status = MemberStatus.Active,
        DateOnly? joinedOn = null,
        Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            CreatedAtUtc = Timestamp,
            UpdatedAtUtc = Timestamp,
            MemberNumber = number,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = new(1990, 1, 1),
            Email = "demo@example.invalid",
            Phone = "+39 000 000 0000",
            JoinedOn = joinedOn ?? new(2026, 1, 1),
            Status = status,
            MedicalCertificateExpiresOn = new(2026, 12, 31),
            PrivacyConsent = true
        };
}
