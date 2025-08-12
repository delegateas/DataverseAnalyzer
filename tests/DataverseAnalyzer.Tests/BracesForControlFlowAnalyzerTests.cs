using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataverseAnalyzer.Tests;

public sealed class BracesForControlFlowAnalyzerTests
{
    [Fact]
    public async Task IfStatementWithReturnShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                        return;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithThrowShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                        throw new System.Exception();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithBreakShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    while (true)
                    {
                        if (true)
                            break;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithContinueShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    while (true)
                    {
                        if (true)
                            continue;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithYieldBreakShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;
            class TestClass
            {
                public System.Collections.Generic.IEnumerable<int> TestMethod()
                {
                    if (true)
                        yield break;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithMethodCallShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                        DoSomething();
                }
                
                private void DoSomething() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0001", diagnostics[0].Id);
    }

    [Fact]
    public async Task ForStatementWithAssignmentShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    int x = 0;
                    for (int i = 0; i < 10; i++)
                        x = i;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0001", diagnostics[0].Id);
    }

    [Fact]
    public async Task WhileStatementWithReturnShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    while (true)
                        return;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithBracesShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                        DoSomething();
                    }
                }
                
                private void DoSomething() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfStatementWithAssignmentShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    var bla = string.Empty;

                    if (true)
                        bla = "bla";
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0001", diagnostics[0].Id);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var analyzer = new BracesForControlFlowAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0001").ToArray();
    }
}