using LayeredCraft.Sculptor.Generator.Tests.TestKit.Attributes;
using Sculptor;

namespace LayeredCraft.Sculptor.Generator.Tests;

public class SculptorGeneratorTests
{
    private static readonly Dictionary<string, string>? FeatureFlags = new()
    {
        ["InterceptorsPreviewNamespaces"] = "Sculptor.Generated",
        ["InterceptorsNamespaces"] = "Sculptor.Generated",
    };

    [Theory]
    [GeneratorAutoData]
    public async Task GeneratesInterceptor_ForClassAttribute(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/001_ClassAttribute/Repository.cs",
                "Cases/001_ClassAttribute/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }
}