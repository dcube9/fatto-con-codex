namespace ViteKlub.Core;

public static class DemoEnvironment
{
    public const string ApplicationName = "ViteKlub";

    public static IReadOnlyList<string> FunctionalAreas { get; } = Array.AsReadOnly<string>(
    [
        "Dashboard",
        "Iscritti",
        "Abbonamenti",
        "Accessi",
        "Pagamenti",
        "Utenti demo",
        "Gestione demo"
    ]);
}
