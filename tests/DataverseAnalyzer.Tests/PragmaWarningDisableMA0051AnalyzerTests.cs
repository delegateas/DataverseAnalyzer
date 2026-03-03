using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class PragmaWarningDisableMA0051AnalyzerTests
{
    [Fact]
    public async Task PragmaDisableMA0051ShouldTrigger()
    {
        var source = """
            class TestClass
            {
            #pragma warning disable MA0051
                public void TestMethod() { }
            #pragma warning restore MA0051
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0011", diagnostics[0].Id);
    }

    [Fact]
    public async Task PragmaDisableMA0051WithOtherWarningsShouldTrigger()
    {
        var source = """
            class TestClass
            {
            #pragma warning disable MA0051, CS0168
                public void TestMethod() { }
            #pragma warning restore MA0051, CS0168
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0011", diagnostics[0].Id);
    }

    [Fact]
    public async Task PragmaRestoreMA0051ShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
            #pragma warning restore MA0051
                public void TestMethod() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task PragmaDisableOtherWarningShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
            #pragma warning disable CS0168
                public void TestMethod() { }
            #pragma warning restore CS0168
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultiplePragmaDisableMA0051ShouldTriggerMultipleTimes()
    {
        var source = """
            class TestClass
            {
            #pragma warning disable MA0051
                public void TestMethodA() { }
            #pragma warning restore MA0051
            #pragma warning disable MA0051
                public void TestMethodB() { }
            #pragma warning restore MA0051
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0011", d.Id));
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

        var analyzer = new PragmaWarningDisableMA0051Analyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0011").ToArray();
    }
}