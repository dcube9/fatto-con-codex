using ViteKlub.Core;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class DemoEnvironmentTests
{
    [Fact]
    public void FunctionalAreasContainOnlyApprovedScope()
    {
        string[] expectedAreas =
        [
            "Dashboard",
            "Iscritti",
            "Abbonamenti",
            "Accessi",
            "Pagamenti",
            "Utenti demo",
            "Gestione demo"
        ];

        Assert.Equal(expectedAreas, DemoEnvironment.FunctionalAreas.ToArray());
    }
}
