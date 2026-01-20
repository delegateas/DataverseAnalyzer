using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class TargetVariableNamingAnalyzerTests
{
    private const string PluginContextDefinition = """
        namespace Microsoft.Xrm.Sdk
        {
            public class Entity { }
        }

        namespace PluginContext
        {
            public interface IPluginContext
            {
                T GetTarget<T>() where T : Microsoft.Xrm.Sdk.Entity;
                T GetTargetMergedWithPreImage<T>() where T : Microsoft.Xrm.Sdk.Entity;
            }
        }
        """;

    [Fact]
    public async Task GetTargetWithCorrectNameShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var target = context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetTargetWithWrongNameShouldTriggerCT0011()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var entity = context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0011", diagnostics[0].Id);
    }

    [Fact]
    public async Task GetTargetWithExplicitTypeCorrectNameShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    Account target = context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetTargetWithExplicitTypeWrongNameShouldTriggerCT0011()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    Account account = context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0011", diagnostics[0].Id);
    }

    [Fact]
    public async Task GetTargetDiagnosticContainsVariableName()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var foo = context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("foo", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMergedWithCorrectNameShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var merged = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetMergedWithWrongNameShouldTriggerCT0012()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var entity = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task GetMergedWithTargetNameShouldTriggerCT0012()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var target = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task GetMergedWithExplicitTypeCorrectNameShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    Account merged = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetMergedWithExplicitTypeWrongNameShouldTriggerCT0012()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    Account account = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task GetMergedDiagnosticContainsVariableName()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var bar = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("bar", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BothMethodsWithCorrectNamesShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var target = context.GetTarget<Account>();
                    var merged = context.GetTargetMergedWithPreImage<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnrelatedMethodCallShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public string GetTarget<T>() => "";

                public void TestMethod()
                {
                    var something = GetTarget<string>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetTargetAsExpressionStatementShouldNotTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CastGetTargetWithWrongNameShouldTrigger()
    {
        var source = PluginContextDefinition + """

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod(PluginContext.IPluginContext context)
                {
                    var entity = (Account)context.GetTarget<Account>();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0011", diagnostics[0].Id);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new TargetVariableNamingAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id is "CT0011" or "CT0012").ToArray();
    }
}
