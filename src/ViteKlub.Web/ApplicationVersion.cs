using System.Reflection;

namespace ViteKlub.Web;

public static class ApplicationVersion
{
    public static string Display { get; } = $"v{GetVersion()}";

    private static string GetVersion()
    {
        string? informationalVersion = typeof(ApplicationVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return informationalVersion?.Split('+', 2)[0]
            ?? typeof(ApplicationVersion).Assembly.GetName().Version?.ToString(3)
            ?? "unknown";
    }
}
