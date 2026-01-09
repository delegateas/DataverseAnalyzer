using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class PluginDocumentationAnalyzerTests
{
    private const string IPluginDefinition = """
        namespace Microsoft.Xrm.Sdk
        {
            public interface IPlugin
            {
                void Execute(System.IServiceProvider serviceProvider);
            }
        }
        """;

    private const string DocumentedPluginBase = """

        /// <summary>Base plugin class.</summary>
        class Plugin : Microsoft.Xrm.Sdk.IPlugin
        {
            public void Execute(System.IServiceProvider serviceProvider) { }
        }
        """;

    private const string UndocumentedPluginBase = """

        class Plugin : Microsoft.Xrm.Sdk.IPlugin
        {
            public void Execute(System.IServiceProvider serviceProvider) { }
        }
        """;

    [Fact]
    public async Task PluginSubclassWithoutXmlCommentShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassWithEmptySummaryShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <summary></summary>
            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassWithWhitespaceSummaryShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <summary>   </summary>
            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassWithValidSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <summary>Handles account creation.</summary>
            class CreateAccountPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginSubclassWithMultiLineSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <summary>
            /// Handles account creation and validation.
            /// </summary>
            class CreateAccountPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassNotInheritingFromPluginShouldNotTrigger()
    {
        var source = """
            class MyClass { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassInheritingFromNonPluginBaseShouldNotTrigger()
    {
        var source = """
            class BaseClass { }

            class MyClass : BaseClass { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginSubclassWithOnlyRemarksShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <remarks>Some remarks here.</remarks>
            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassWithInheritdocShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <inheritdoc/>
            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginSubclassWithInheritdocCrefShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <inheritdoc cref="Plugin"/>
            class MyPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultiplePluginSubclassesWithMixedDocsShouldTriggerOnlyForMissing()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            class UndocumentedPlugin : Plugin { }

            /// <summary>Documented plugin.</summary>
            class DocumentedPlugin : Plugin { }

            class AnotherUndocumentedPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0006", d.Id));
    }

    [Fact]
    public async Task PluginSubclassWithQualifiedBaseTypeShouldTrigger()
    {
        var source = IPluginDefinition + """

            namespace MyNamespace
            {
                /// <summary>Base plugin.</summary>
                class Plugin : Microsoft.Xrm.Sdk.IPlugin
                {
                    public void Execute(System.IServiceProvider serviceProvider) { }
                }
            }

            class MyPlugin : MyNamespace.Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassWithQualifiedBaseTypeAndSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + """

            namespace MyNamespace
            {
                /// <summary>Base plugin.</summary>
                class Plugin : Microsoft.Xrm.Sdk.IPlugin
                {
                    public void Execute(System.IServiceProvider serviceProvider) { }
                }
            }

            /// <summary>My plugin.</summary>
            class MyPlugin : MyNamespace.Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedPluginSubclassShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            class OuterClass
            {
                class NestedPlugin : Plugin { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task NestedPluginSubclassWithSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            class OuterClass
            {
                /// <summary>Nested plugin.</summary>
                class NestedPlugin : Plugin { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginBaseClassItselfShouldTrigger()
    {
        var source = IPluginDefinition + UndocumentedPluginBase;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task ClassWithPluginInNameButNotInheritingShouldNotTrigger()
    {
        var source = """
            class MyPluginHelper { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginSubclassWithSummaryAndOtherTagsShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            /// <summary>Handles account creation.</summary>
            /// <remarks>Additional details here.</remarks>
            class CreateAccountPlugin : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PluginSubclassImplementingInterfaceShouldTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            interface IMyInterface { }

            class MyPlugin : Plugin, IMyInterface { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task PluginSubclassImplementingInterfaceWithSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            interface IMyInterface { }

            /// <summary>My plugin.</summary>
            class MyPlugin : Plugin, IMyInterface { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DiagnosticContainsClassName()
    {
        var source = IPluginDefinition + DocumentedPluginBase + """

            class TestPluginName : Plugin { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("TestPluginName", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClassDirectlyImplementingIPluginShouldTrigger()
    {
        var source = IPluginDefinition + """

            class MyDirectPlugin : Microsoft.Xrm.Sdk.IPlugin
            {
                public void Execute(System.IServiceProvider serviceProvider) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0006", diagnostics[0].Id);
    }

    [Fact]
    public async Task ClassDirectlyImplementingIPluginWithSummaryShouldNotTrigger()
    {
        var source = IPluginDefinition + """

            /// <summary>Direct IPlugin implementation.</summary>
            class MyDirectPlugin : Microsoft.Xrm.Sdk.IPlugin
            {
                public void Execute(System.IServiceProvider serviceProvider) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassInheritingFromNonIPluginPluginClassShouldNotTrigger()
    {
        var source = """
            class Plugin { }

            class MyPlugin : Plugin { }
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

        var analyzer = new PluginDocumentationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0006").ToArray();
    }
}