using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class PluginStepConfigurationAnalyzerTests
{
    private const string PluginStepApiDefinition = """
        namespace PluginRegistration
        {
            public enum EventOperation { Create, Update, Delete }
            public enum ImageType { PreImage, PostImage, Both }

            public class PluginStepBuilder
            {
                public PluginStepBuilder AddFilteredAttributes(params string[] attributes) => this;
                public PluginStepBuilder AddImage(ImageType type, params string[] attributes) => this;
            }

            public static class PluginRegistrar
            {
                public static PluginStepBuilder RegisterPluginStep<T>(EventOperation op, string stage, System.Action<object> handler)
                    => new PluginStepBuilder();
            }
        }
        """;

    [Fact]
    public async Task AddFilteredAttributesOnCreateShouldTriggerCT0008()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PreOperation", p => { })
                        .AddFilteredAttributes("name", "address");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0008", diagnostics[0].Id);
    }

    [Fact]
    public async Task AddFilteredAttributesOnUpdateShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Update, "PreOperation", p => { })
                        .AddFilteredAttributes("name", "address");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddPreImageOnCreateShouldTriggerCT0009()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PreOperation", p => { })
                        .AddImage(ImageType.PreImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0009", diagnostics[0].Id);
    }

    [Fact]
    public async Task AddBothImageOnCreateShouldTriggerCT0009()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PreOperation", p => { })
                        .AddImage(ImageType.Both, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0009", diagnostics[0].Id);
    }

    [Fact]
    public async Task AddPostImageOnCreateShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PostOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddPostImageOnDeleteShouldTriggerCT0010()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Delete, "PreOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0010", diagnostics[0].Id);
    }

    [Fact]
    public async Task AddBothImageOnDeleteShouldTriggerCT0010()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Delete, "PreOperation", p => { })
                        .AddImage(ImageType.Both, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0010", diagnostics[0].Id);
    }

    [Fact]
    public async Task AddPreImageOnDeleteShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Delete, "PreOperation", p => { })
                        .AddImage(ImageType.PreImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddPreImageOnUpdateShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Update, "PreOperation", p => { })
                        .AddImage(ImageType.PreImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddPostImageOnUpdateShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Update, "PreOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddBothImageOnUpdateShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Update, "PreOperation", p => { })
                        .AddImage(ImageType.Both, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ChainedAddFilteredAttributesThenPreImageOnCreateShouldTriggerBoth()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PreOperation", p => { })
                        .AddFilteredAttributes("name")
                        .AddImage(ImageType.PreImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.Contains(diagnostics, d => d.Id == "CT0008");
        Assert.Contains(diagnostics, d => d.Id == "CT0009");
    }

    [Fact]
    public async Task MultipleStepsWithDifferentOperationsShouldOnlyTriggerOnInvalid()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PostOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");

                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Delete, "PreOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0010", diagnostics[0].Id);
    }

    [Fact]
    public async Task CT0009DiagnosticContainsImageTypeName()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Create, "PreOperation", p => { })
                        .AddImage(ImageType.Both, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("Both", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CT0010DiagnosticContainsImageTypeName()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void Configure()
                {
                    PluginRegistrar.RegisterPluginStep<object>(EventOperation.Delete, "PreOperation", p => { })
                        .AddImage(ImageType.PostImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("PostImage", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddImageWithoutRegisterPluginStepShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void TestMethod(PluginStepBuilder builder)
                {
                    builder.AddImage(ImageType.PreImage, "name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AddFilteredAttributesWithoutRegisterPluginStepShouldNotTrigger()
    {
        var source = PluginStepApiDefinition + """

            using PluginRegistration;

            class TestClass
            {
                public void TestMethod(PluginStepBuilder builder)
                {
                    builder.AddFilteredAttributes("name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new PluginStepConfigurationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id is "CT0008" or "CT0009" or "CT0010").ToArray();
    }
}