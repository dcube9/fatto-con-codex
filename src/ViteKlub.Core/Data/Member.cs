namespace ViteKlub.Core.Data;

public sealed record Member : DemoEntity
{
    public required string MemberNumber { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required DateOnly DateOfBirth { get; init; }

    public required string Email { get; init; }

    public required string Phone { get; init; }

    public required DateOnly JoinedOn { get; init; }

    public required MemberStatus Status { get; init; }

    public DateOnly? MedicalCertificateExpiresOn { get; init; }

    public string? EmergencyContact { get; init; }

    public string? Notes { get; init; }

    public bool PrivacyConsent { get; init; }
}
