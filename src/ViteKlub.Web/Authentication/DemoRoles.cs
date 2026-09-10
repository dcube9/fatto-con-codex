namespace ViteKlub.Web.Authentication;

public static class DemoRoles
{
    public const string Administrator = nameof(Administrator);
    public const string Manager = nameof(Manager);
    public const string Receptionist = nameof(Receptionist);
    public const string Viewer = nameof(Viewer);

    public const string All = $"{Administrator},{Manager},{Receptionist},{Viewer}";
    public const string Operational = $"{Administrator},{Manager},{Receptionist},{Viewer}";
    public const string Payments = $"{Administrator},{Manager},{Receptionist}";
    public const string Administration = Administrator;
}
