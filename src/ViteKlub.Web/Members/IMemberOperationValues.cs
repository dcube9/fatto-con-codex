using ViteKlub.Core.Data;
using ViteKlub.Core.Members;

namespace ViteKlub.Web.Members;

/// <summary>Fornisce i valori non deterministici necessari a un comando sugli iscritti.</summary>
public interface IMemberOperationValues
{
    DateTimeOffset GetTimestamp();

    Guid NewMemberId();

    Guid NewAuditEventId();
}

public sealed class SystemMemberOperationValues : IMemberOperationValues
{
    public DateTimeOffset GetTimestamp() => DateTimeOffset.UtcNow;

    public Guid NewMemberId() => Guid.NewGuid();

    public Guid NewAuditEventId() => Guid.NewGuid();
}

public sealed class MemberCommandFactory(IMemberOperationValues values)
{
    public MemberOperationResult Create(DemoDataset dataset, MemberInput input, Guid actorUserId) =>
        MemberManagement.Create(
            dataset,
            input,
            MemberManagement.NextMemberNumber(dataset.Members),
            values.NewMemberId(),
            values.NewAuditEventId(),
            actorUserId,
            values.GetTimestamp());

    public MemberOperationResult Update(
        DemoDataset dataset,
        Guid memberId,
        int expectedVersion,
        MemberInput input,
        Guid actorUserId) =>
        MemberManagement.Update(
            dataset,
            memberId,
            expectedVersion,
            input,
            values.NewAuditEventId(),
            actorUserId,
            values.GetTimestamp());

    public MemberOperationResult ChangeStatus(
        DemoDataset dataset,
        Guid memberId,
        int expectedVersion,
        MemberStatus target,
        Guid actorUserId) =>
        MemberManagement.ChangeStatus(
            dataset,
            memberId,
            expectedVersion,
            target,
            values.NewAuditEventId(),
            actorUserId,
            values.GetTimestamp());
}
