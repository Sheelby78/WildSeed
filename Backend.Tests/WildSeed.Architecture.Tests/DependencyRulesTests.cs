using System.Reflection;
using WildSeed.Api;
using WildSeed.Domain;
using WildSeed.Simulation;

namespace WildSeed.Architecture.Tests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Domain_does_not_reference_outer_layers()
    {
        var references = ReferencedAssemblies(typeof(DomainAssemblyMarker));

        Assert.DoesNotContain("WildSeed.Simulation", references);
        Assert.DoesNotContain("WildSeed.Api", references);
    }

    [Fact]
    public void Simulation_does_not_reference_api()
    {
        var references = ReferencedAssemblies(typeof(SimulationAssemblyMarker));

        Assert.DoesNotContain("WildSeed.Api", references);
    }

    [Fact]
    public void Api_boundary_remains_the_outermost_layer()
    {
        var apiAssemblyName = typeof(ApiAssemblyMarker).Assembly.GetName().Name;

        Assert.Equal("WildSeed.Api", apiAssemblyName);
    }

    private static IReadOnlySet<string?> ReferencedAssemblies(Type markerType) =>
        markerType.Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToHashSet(StringComparer.Ordinal);
}
