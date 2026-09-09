namespace ViteKlub.Core.Data;

public enum MemberStatus
{
    Active,
    Suspended,
    Archived
}

public enum MembershipPlanType
{
    TimeBased,
    EntryBased
}

public enum SubscriptionStatus
{
    Scheduled,
    Active,
    Suspended,
    Expired,
    Cancelled
}

public enum AccessOutcome
{
    Granted,
    Denied,
    Cancelled
}

public enum AccessSource
{
    FrontDesk,
    Simulator
}

public enum AccessDenialReason
{
    None,
    MemberUnavailable,
    NoValidSubscription,
    SubscriptionSuspended,
    EntriesExhausted,
    MedicalCertificateExpired,
    DuplicateCheckIn
}

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    Other
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public enum DemoRole
{
    Administrator,
    Manager,
    Receptionist,
    Viewer
}
