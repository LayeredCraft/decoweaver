using DecoWeaver;
using LayeredCraft.DecoWeaver.Generator.Tests.TestKit.Attributes;

namespace LayeredCraft.DecoWeaver.Generator.Tests;

public class DecoWeaverGeneratorTests
{
    private static readonly Dictionary<string, string>? FeatureFlags = new()
    {
        ["InterceptorsPreviewNamespaces"] = "DecoWeaver.Generated",
        ["InterceptorsNamespaces"] = "DecoWeaver.Generated",
    };

    [Theory]
    [GeneratorAutoData]
    public async Task OpenGeneric_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_MultipleOrdered_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_ConstructorOrderSyntax_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_MultipleDefaultOrder_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_MixedOrderSyntax_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_NonGenericDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_IsInterceptableFalse_NoInterceptorGenerated(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_NoDecorators_PassThrough(DecoWeaverGenerator sut)
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
    public async Task OpenGeneric_ThreeDecorators_GeneratesCorrectNesting(DecoWeaverGenerator sut)
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
    public async Task Generic_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Generic_MultipleOrdered_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Generic_MixedSyntax_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Generic_IsInterceptableFalse_NoInterceptorGenerated(DecoWeaverGenerator sut)
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
    public async Task Transient_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Transient_MultipleDecorators_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Singleton_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Singleton_MultipleDecorators_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Transient_Generic_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Transient_Generic_MultipleDecorators_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Singleton_Generic_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
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
    public async Task Singleton_Generic_MultipleDecorators_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/021_Singleton_Generic_MultipleDecorators/Repository.cs",
                "Cases/021_Singleton_Generic_MultipleDecorators/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task FactoryDelegate_ShouldNotIntercept(DecoWeaverGenerator sut)
    {
        // This test verifies that registrations with factory delegates are NOT intercepted
        // Only the parameterless overload AddScoped<T1, T2>() should be intercepted
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/022_FactoryDelegate_ShouldNotIntercept/Repository.cs",
                "Cases/022_FactoryDelegate_ShouldNotIntercept/Program.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task ServiceDecorator_SingleDecorator_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/023_ServiceDecorator_SingleDecorator/Repository.cs",
                "Cases/023_ServiceDecorator_SingleDecorator/Program.cs",
                "Cases/023_ServiceDecorator_SingleDecorator/Global.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task ServiceDecorator_MultipleOrdered_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/024_ServiceDecorator_MultipleOrdered/Repository.cs",
                "Cases/024_ServiceDecorator_MultipleOrdered/Program.cs",
                "Cases/024_ServiceDecorator_MultipleOrdered/Global.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task ServiceDecorator_WithSkipAssembly_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/025_ServiceDecorator_WithSkipAssembly/Repository.cs",
                "Cases/025_ServiceDecorator_WithSkipAssembly/Program.cs",
                "Cases/025_ServiceDecorator_WithSkipAssembly/Global.cs"
            ],
            featureFlags: FeatureFlags);
    }

    [Theory]
    [GeneratorAutoData]
    public async Task ServiceDecorator_SkipAssemblyWithClassLevel_GeneratesCorrectInterceptor(DecoWeaverGenerator sut)
    {
        await VerifyGlue.VerifySourcesAsync(sut,
            [
                "Cases/026_SkipAssemblyWithClassLevel/Repository.cs",
                "Cases/026_SkipAssemblyWithClassLevel/Program.cs",
                "Cases/026_SkipAssemblyWithClassLevel/Global.cs"
            ],
            featureFlags: FeatureFlags);
    }
}