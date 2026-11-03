using System.Reflection;
using CiroApp.Application;
using CiroApp.Domain.Models;

namespace CiroApp.Tests.Architecture;

public class LayerDependencyTests
{
    private static string[] CiroReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("CiroApp."))
            .ToArray();

    [Fact]
    public void Domain_HicbirCiroKatmaninaBagliDegildir()
    {
        Assert.Empty(CiroReferences(typeof(CiroDay).Assembly));
    }

    [Fact]
    public void Application_YalnizcaDomaineBaglidir()
    {
        var refs = CiroReferences(typeof(ApplicationAssemblyMarker).Assembly);
        Assert.All(refs, r => Assert.Equal("CiroApp.Domain", r));
    }
}
