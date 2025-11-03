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
    public async Task OpenGeneric_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/001_OpenGeneric_SingleDecorator/Repository.cs",
                "Cases/001_OpenGeneric_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_MultipleOrdered_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/002_OpenGeneric_MultipleOrdered/Repository.cs",
                "Cases/002_OpenGeneric_MultipleOrdered/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_ConstructorOrderSyntax_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/003_OpenGeneric_ConstructorOrderSyntax/Repository.cs",
                "Cases/003_OpenGeneric_ConstructorOrderSyntax/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_MultipleDefaultOrder_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/004_OpenGeneric_MultipleDefaultOrder/Repository.cs",
                "Cases/004_OpenGeneric_MultipleDefaultOrder/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_MixedOrderSyntax_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/005_OpenGeneric_MixedOrderSyntax/Repository.cs",
                "Cases/005_OpenGeneric_MixedOrderSyntax/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_NonGenericDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/006_OpenGeneric_NonGenericDecorator/Repository.cs",
                "Cases/006_OpenGeneric_NonGenericDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_IsInterceptableFalse_NoInterceptorGenerated(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/007_OpenGeneric_IsInterceptableFalse/Repository.cs",
                "Cases/007_OpenGeneric_IsInterceptableFalse/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_NoDecorators_PassThrough(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/008_OpenGeneric_NoDecorators/Repository.cs",
                "Cases/008_OpenGeneric_NoDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_ThreeDecorators_GeneratesCorrectNesting(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/009_OpenGeneric_ThreeDecorators/Repository.cs",
                "Cases/009_OpenGeneric_ThreeDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Generic_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/010_Generic_SingleDecorator/Repository.cs",
                "Cases/010_Generic_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Generic_MultipleOrdered_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/011_Generic_MultipleOrdered/Repository.cs",
                "Cases/011_Generic_MultipleOrdered/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Generic_MixedSyntax_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/012_Generic_MixedSyntax/Repository.cs",
                "Cases/012_Generic_MixedSyntax/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Generic_IsInterceptableFalse_NoInterceptorGenerated(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/013_Generic_IsInterceptableFalse/Repository.cs",
                "Cases/013_Generic_IsInterceptableFalse/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Transient_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/014_Transient_SingleDecorator/Repository.cs",
                "Cases/014_Transient_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Transient_MultipleDecorators_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/015_Transient_MultipleDecorators/Repository.cs",
                "Cases/015_Transient_MultipleDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Singleton_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/016_Singleton_SingleDecorator/Repository.cs",
                "Cases/016_Singleton_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Singleton_MultipleDecorators_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/017_Singleton_MultipleDecorators/Repository.cs",
                "Cases/017_Singleton_MultipleDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Transient_Generic_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/018_Transient_Generic_SingleDecorator/Repository.cs",
                "Cases/018_Transient_Generic_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Transient_Generic_MultipleDecorators_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/019_Transient_Generic_MultipleDecorators/Repository.cs",
                "Cases/019_Transient_Generic_MultipleDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Singleton_Generic_SingleDecorator_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/020_Singleton_Generic_SingleDecorator/Repository.cs",
                "Cases/020_Singleton_Generic_SingleDecorator/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task Singleton_Generic_MultipleDecorators_GeneratesCorrectInterceptor(SculptorGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/021_Singleton_Generic_MultipleDecorators/Repository.cs",
                "Cases/021_Singleton_Generic_MultipleDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }
}