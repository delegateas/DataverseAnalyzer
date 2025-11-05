using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataverseAnalyzer.Tests;

public sealed class NoBracesForControlFlowAnalyzerTests
{
    [Fact]
    public async Task BlockWithOnlyReturnShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                        return;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0004", diagnostics[0].Id);
    }

    [Fact]
    public async Task BlockWithOnlyThrowShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                        throw new System.Exception();
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0004", diagnostics[0].Id);
    }

    [Fact]
    public async Task BlockWithOnlyBreakShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    while (true)
                    {
                        if (true)
                        {
                            break;
                        }
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0004", diagnostics[0].Id);
    }

    [Fact]
    public async Task BlockWithOnlyContinueShouldTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    while (true)
                    {
                        if (true)
                        {
                            continue;
                        }
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0004", diagnostics[0].Id);
    }

    [Fact]
    public async Task BlockWithOnlyYieldBreakShouldTrigger()
    {
        var source = """
            using System.Collections.Generic;
            class TestClass
            {
                public System.Collections.Generic.IEnumerable<int> TestMethod()
                {
                    if (true)
                    {
                        yield break;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0004", diagnostics[0].Id);
    }

    [Fact]
    public async Task BlockWithMethodCallShouldNotTrigger()
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
    public async Task BlockWithMultipleStatementsShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                        DoSomething();
                        return;
                    }
                }
                
                private void DoSomething() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StatementWithoutBracesShouldNotTrigger()
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
    public async Task BlockWithAssignmentShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    var bla = string.Empty;

                    if (true)
                    {
                        bla = "bla";
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ElseClauseWithOnlyReturnShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                        return;
                    else
                    {
                        return;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfWithElseIfShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition1, bool condition2)
                {
                    if (condition1)
                    {
                        return;
                    }
                    else if (condition2)
                    {
                        return;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BlockWithYieldReturnShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;
            class TestClass
            {
                public System.Collections.Generic.IEnumerable<int> TestMethod()
                {
                    if (true)
                    {
                        yield return 1;
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EmptyBlockShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfElseWithMultipleStatementsInIfAndSingleControlFlowInElseShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod(bool condition)
                {
                    if (condition)
                    {
                        DoSomething();
                        DoSomethingElse();
                    }
                    else
                    {
                        throw new System.Exception();
                    }
                }
                
                private void DoSomething() { }
                private void DoSomethingElse() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var analyzer = new NoBracesForControlFlowAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0004").ToArray();
    }
}