using System.Reflection;
using ViteKlub.Web;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class ApplicationVersionTests
{
    [Fact]
    public void DisplayUsesVersionPrefixWithoutBuildMetadata()
    {
        string informationalVersion = typeof(ApplicationVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

        Assert.Equal($"v{informationalVersion.Split('+', 2)[0]}", ApplicationVersion.Display);
        Assert.Equal("v0.2.0", ApplicationVersion.Display);
    }
}
