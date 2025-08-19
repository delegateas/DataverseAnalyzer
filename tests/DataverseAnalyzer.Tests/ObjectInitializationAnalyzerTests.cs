using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataverseAnalyzer.Tests;

public sealed class ObjectInitializationAnalyzerTests
{
    [Fact]
    public async Task ObjectCreationWithEmptyParenthesesAndInitializerShouldTrigger()
    {
        var source = """
            class Account
            {
                public string Name { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account()
                    {
                        Name = "MoneyMan",
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0003", diagnostics[0].Id);
    }

    [Fact]
    public async Task ObjectCreationWithoutParenthesesAndInitializerShouldNotTrigger()
    {
        var source = """
            class Account
            {
                public string Name { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account
                    {
                        Name = "MoneyMan",
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCreationWithParametersAndInitializerShouldNotTrigger()
    {
        var source = """
            class Account
            {
                public string Name { get; set; }
                public Account(string id) { }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var accountId = "123";
                    var account = new Account(accountId)
                    {
                        Name = "MoneyMan",
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCreationWithEmptyParenthesesWithoutInitializerShouldNotTrigger()
    {
        var source = """
            class Account
            {
                public string Name { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BuiltInTypeWithEmptyParenthesesAndInitializerShouldTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                public void TestMethod()
                {
                    var list = new List<string>()
                    {
                        "item1",
                        "item2"
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0003", diagnostics[0].Id);
    }

    [Fact]
    public async Task AnonymousTypeCreationShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    var obj = new
                    {
                        Name = "Test",
                        Value = 42
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleViolationsInSameMethodShouldTriggerMultiple()
    {
        var source = """
            class Account
            {
                public string Name { get; set; }
            }

            class Person
            {
                public string FirstName { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account()
                    {
                        Name = "MoneyMan",
                    };

                    var person = new Person()
                    {
                        FirstName = "John",
                    };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0003", d.Id));
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var analyzer = new ObjectInitializationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0003").ToArray();
    }
}