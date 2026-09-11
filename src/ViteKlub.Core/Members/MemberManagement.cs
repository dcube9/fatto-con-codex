using System.Net.Mail;
using System.Numerics;
using System.Text.RegularExpressions;
using ViteKlub.Core.Data;

namespace ViteKlub.Core.Members;

public sealed record MemberInput(
    string? FirstName,
    string? LastName,
    DateOnly DateOfBirth,
    string? Email,
    string? Phone,
    DateOnly JoinedOn,
    DateOnly? MedicalCertificateExpiresOn = null,
    string? EmergencyContact = null,
    string? Notes = null,
    bool PrivacyConsent = false);

public sealed record MemberOperationResult(DemoDataset Dataset, Member Member);

public sealed class MemberValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("I dati dell’iscritto non sono validi.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public static partial class MemberManagement
{
    public const string AuditEntityType = "Member";
    private const int MaximumNameLength = 100;
    private const int MaximumEmailLength = 254;
    private const int MaximumPhoneLength = 50;
    private const int MaximumOptionalLength = 1000;

    public static string NextMemberNumber(IEnumerable<Member> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        HashSet<BigInteger> used = members
            .Select(member => MemberNumberPattern().Match(member.MemberNumber))
            .Where(match => match.Success && BigInteger.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out BigInteger value) && value > BigInteger.Zero)
            .Select(match => BigInteger.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToHashSet();
        BigInteger next = BigInteger.One;
        while (used.Contains(next))
        {
            next++;
        }

        return $"VK-{next.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(5, '0')}";
    }

    public static MemberOperationResult Create(
        DemoDataset source,
        MemberInput input,
        Guid memberId,
        Guid auditEventId,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Create(source, input, NextMemberNumber(source.Members), memberId, auditEventId, actorUserId, occurredAtUtc);
    }

    public static MemberOperationResult Create(
        DemoDataset source,
        MemberInput input,
        string memberNumber,
        Guid memberId,
        Guid auditEventId,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureIds(source, memberId, auditEventId, actorUserId);
        MemberInput normalized = Normalize(input);
        Validate(normalized, DateOnly.FromDateTime(occurredAtUtc.UtcDateTime), true);
        if (string.IsNullOrWhiteSpace(memberNumber) ||
            source.Members.Any(member => string.Equals(member.MemberNumber, memberNumber, StringComparison.OrdinalIgnoreCase)))
        {
            throw Error("MemberNumber", "Il numero tessera è già utilizzato.");
        }

        var member = new Member
        {
            Id = memberId,
            MemberNumber = memberNumber,
            FirstName = normalized.FirstName!,
            LastName = normalized.LastName!,
            DateOfBirth = normalized.DateOfBirth,
            Email = normalized.Email!,
            Phone = normalized.Phone!,
            JoinedOn = normalized.JoinedOn,
            MedicalCertificateExpiresOn = normalized.MedicalCertificateExpiresOn,
            EmergencyContact = normalized.EmergencyContact,
            Notes = normalized.Notes,
            PrivacyConsent = normalized.PrivacyConsent,
            Status = MemberStatus.Active,
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc,
            Version = 1
        };
        return Result(source, member, "member.created", "Iscritto creato", auditEventId, actorUserId, occurredAtUtc, false);
    }

    public static MemberOperationResult Update(
        DemoDataset source, Guid memberId, int expectedVersion, MemberInput input,
        Guid auditEventId, Guid actorUserId, DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        Member current = Find(source, memberId);
        EnsureActorAndAuditId(source, auditEventId, actorUserId);
        if (current.Status == MemberStatus.Archived)
        {
            throw Error("Status", "Un iscritto archiviato non può essere modificato.");
        }
        if (current.Version != expectedVersion)
        {
            throw Error("Version", "I dati sono stati aggiornati in un’altra operazione. Ricarica e riprova.");
        }

        MemberInput normalized = Normalize(input);
        Validate(normalized, DateOnly.FromDateTime(occurredAtUtc.UtcDateTime), false);
        Member updated = current with
        {
            FirstName = normalized.FirstName!,
            LastName = normalized.LastName!,
            DateOfBirth = normalized.DateOfBirth,
            Email = normalized.Email!,
            Phone = normalized.Phone!,
            JoinedOn = normalized.JoinedOn,
            MedicalCertificateExpiresOn = normalized.MedicalCertificateExpiresOn,
            EmergencyContact = normalized.EmergencyContact,
            Notes = normalized.Notes,
            PrivacyConsent = normalized.PrivacyConsent,
            UpdatedAtUtc = occurredAtUtc,
            Version = current.Version + 1
        };
        return Result(source, updated, "member.updated", "Iscritto modificato", auditEventId, actorUserId, occurredAtUtc, true);
    }

    public static MemberOperationResult ChangeStatus(
        DemoDataset source, Guid memberId, int expectedVersion, MemberStatus target,
        Guid auditEventId, Guid actorUserId, DateTimeOffset occurredAtUtc)
    {
        Member current = Find(source, memberId);
        EnsureActorAndAuditId(source, auditEventId, actorUserId);
        if (current.Version != expectedVersion)
        {
            throw Error("Version", "I dati sono stati aggiornati in un’altra operazione. Ricarica e riprova.");
        }
        if (!CanTransition(current.Status, target))
        {
            throw Error("Status", "Il cambio di stato richiesto non è consentito.");
        }

        string action = target switch
        {
            MemberStatus.Active => "member.reactivated",
            MemberStatus.Suspended => "member.suspended",
            MemberStatus.Archived => "member.archived",
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        string summary = target switch
        {
            MemberStatus.Active => "Iscritto riattivato",
            MemberStatus.Suspended => "Iscritto sospeso",
            _ => "Iscritto archiviato"
        };
        Member updated = current with { Status = target, UpdatedAtUtc = occurredAtUtc, Version = current.Version + 1 };
        return Result(source, updated, action, summary, auditEventId, actorUserId, occurredAtUtc, true);
    }

    public static bool CanTransition(MemberStatus current, MemberStatus target) =>
        (current, target) is (MemberStatus.Active, MemberStatus.Suspended or MemberStatus.Archived)
            or (MemberStatus.Suspended, MemberStatus.Active or MemberStatus.Archived);

    private static MemberOperationResult Result(DemoDataset source, Member member, string action, string summary,
        Guid auditId, Guid actorId, DateTimeOffset timestamp, bool replace)
    {
        Member[] members = (replace ? source.Members.Where(item => item.Id != member.Id).Append(member) : source.Members.Append(member))
            .OrderBy(item => item.MemberNumber, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Id).ToArray();
        var audit = new AuditEvent
        {
            Id = auditId,
            ActorUserId = actorId,
            Action = action,
            EntityType = AuditEntityType,
            EntityId = member.Id,
            Summary = summary,
            OccurredAtUtc = timestamp,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            Version = 1
        };
        DemoDataset result = source with
        {
            Members = members,
            AuditEvents = source.AuditEvents.Append(audit).OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id).ToArray()
        };
        return new(result, member);
    }

    private static Member Find(DemoDataset source, Guid id) => source.Members.SingleOrDefault(member => member.Id == id)
        ?? throw Error("Id", "L’iscritto richiesto non esiste più.");

    private static void EnsureIds(DemoDataset source, Guid memberId, Guid auditId, Guid actorId)
    {
        EnsureActorAndAuditId(source, auditId, actorId);
        if (memberId == Guid.Empty || source.Members.Any(member => member.Id == memberId))
        {
            throw Error("Id", "L’identificativo del nuovo iscritto non è valido o è già utilizzato.");
        }
    }

    private static void EnsureActorAndAuditId(DemoDataset source, Guid auditId, Guid actorId)
    {
        if (!source.Users.Any(user => user.Id == actorId)) throw Error("Actor", "L’utente demo non è valido.");
        if (auditId == Guid.Empty || source.AuditEvents.Any(item => item.Id == auditId)) throw Error("Audit", "L’identificativo audit non è valido o è già utilizzato.");
    }

    private static MemberInput Normalize(MemberInput input) => input with
    {
        FirstName = NormalizeRequired(input.FirstName),
        LastName = NormalizeRequired(input.LastName),
        Email = NormalizeRequired(input.Email),
        Phone = NormalizeRequired(input.Phone),
        EmergencyContact = NormalizeOptional(input.EmergencyContact),
        Notes = NormalizeOptional(input.Notes)
    };

    private static string NormalizeRequired(string? value) => string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static string? NormalizeOptional(string? value) { string result = NormalizeRequired(value); return result.Length == 0 ? null : result; }

    private static void Validate(MemberInput input, DateOnly operationDate, bool creating)
    {
        var errors = new Dictionary<string, string[]>();
        Required(input.FirstName, "FirstName", "Il nome è obbligatorio.", MaximumNameLength, errors);
        Required(input.LastName, "LastName", "Il cognome è obbligatorio.", MaximumNameLength, errors);
        Required(input.Phone, "Phone", "Il telefono è obbligatorio.", MaximumPhoneLength, errors);
        Required(input.Email, "Email", "L’email è obbligatoria.", MaximumEmailLength, errors);
        if (!string.IsNullOrEmpty(input.Email) && !MailAddress.TryCreate(input.Email, out _)) errors["Email"] = ["Inserisci un indirizzo email valido."];
        if (input.DateOfBirth == default) errors["DateOfBirth"] = ["La data di nascita è obbligatoria."];
        else if (input.DateOfBirth >= operationDate) errors["DateOfBirth"] = ["La data di nascita deve precedere la data operativa."];
        if (input.JoinedOn == default) errors["JoinedOn"] = ["La data di iscrizione è obbligatoria."];
        else if (input.JoinedOn < input.DateOfBirth) errors["JoinedOn"] = ["La data di iscrizione non può precedere la data di nascita."];
        if (creating && !input.PrivacyConsent) errors["PrivacyConsent"] = ["Il consenso privacy è obbligatorio."];
        if (input.EmergencyContact?.Length > MaximumOptionalLength) errors["EmergencyContact"] = ["Il contatto di emergenza è troppo lungo."];
        if (input.Notes?.Length > MaximumOptionalLength) errors["Notes"] = ["Le note sono troppo lunghe."];
        if (errors.Count > 0) throw new MemberValidationException(errors);
    }

    private static void Required(string? value, string field, string message, int maximum, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrEmpty(value)) errors[field] = [message];
        else if (value.Length > maximum) errors[field] = [$"Il campo non può superare {maximum} caratteri."];
    }

    private static MemberValidationException Error(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });

    [GeneratedRegex("^VK-(\\d+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex MemberNumberPattern();
}
