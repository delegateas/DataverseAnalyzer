using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataverseAnalyzer.Tests;

public sealed class EnumAssignmentAnalyzerTests
{
    [Fact]
    public async Task EnumPropertyAssignedEnumValueShouldNotTrigger()
    {
        var source = """
            public enum AccountCategoryCode
            {
                Standard = 1,
                Preferred = 2
            }

            class TestClass
            {
                public AccountCategoryCode? AccountCategoryCode { get; set; }

                public void TestMethod()
                {
                    AccountCategoryCode = AccountCategoryCode.Standard;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumPropertyAssignedLiteralShouldTrigger()
    {
        var source = """
            public enum AccountCategoryCode
            {
                Standard = 1,
                Preferred = 2
            }

            class TestClass
            {
                public AccountCategoryCode? AccountCategoryCode { get; set; }

                public void TestMethod()
                {
                    AccountCategoryCode = 2;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task NonNullableEnumPropertyAssignedLiteralShouldTrigger()
    {
        var source = """
            public enum Status
            {
                Active = 1,
                Inactive = 2
            }

            class TestClass
            {
                public Status Status { get; set; }

                public void TestMethod()
                {
                    Status = 1;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task PropertyInitializerWithLiteralShouldTrigger()
    {
        var source = """
            public enum Priority
            {
                Low = 1,
                High = 2
            }

            class TestClass
            {
                public Priority Priority { get; set; } = 1;
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task PropertyInitializerWithEnumValueShouldNotTrigger()
    {
        var source = """
            public enum Priority
            {
                Low = 1,
                High = 2
            }

            class TestClass
            {
                public Priority Priority { get; set; } = Priority.Low;
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonEnumPropertyAssignedLiteralShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public int Number { get; set; }

                public void TestMethod()
                {
                    Number = 42;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumFieldAssignedLiteralShouldTrigger()
    {
        var source = """
            public enum Color
            {
                Red = 1,
                Blue = 2
            }

            class TestClass
            {
                private Color color;

                public void TestMethod()
                {
                    color = 2;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task EnumPropertyAssignedVariableShouldNotTrigger()
    {
        var source = """
            public enum Status
            {
                Active = 1,
                Inactive = 2
            }

            class TestClass
            {
                public Status Status { get; set; }

                public void TestMethod()
                {
                    var value = 1;
                    Status = value;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumPropertyAssignedCastShouldTrigger()
    {
        var source = """
            public enum Status
            {
                Active = 1,
                Inactive = 2
            }

            class TestClass
            {
                public Status Status { get; set; }

                public void TestMethod()
                {
                    Status = (Status)1;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task EnumCastWithDataverseNamingPatternShouldTrigger()
    {
        var source = """
            public enum demo_Entity_statuscode
            {
                ValidStatus = 1,
                InvalidStatus = 2
            }

            class TestClass
            {
                public demo_Entity_statuscode statuscode { get; set; }

                public void TestMethod()
                {
                    statuscode = (demo_Entity_statuscode)3;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task PropertyInitializerWithCastShouldTrigger()
    {
        var source = """
            public enum Priority
            {
                Low = 1,
                High = 2
            }

            class TestClass
            {
                public Priority Priority { get; set; } = (Priority)1;
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task MemberAccessEnumAssignedCastShouldTrigger()
    {
        var source = """
            public enum AccountCategoryCode
            {
                Standard = 1,
                Preferred = 2
            }

            class Account
            {
                public AccountCategoryCode? AccountCategoryCode { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                    account.AccountCategoryCode = (AccountCategoryCode)2;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    [Fact]
    public async Task EnumCastFromVariableShouldNotTrigger()
    {
        var source = """
            public enum Status
            {
                Active = 1,
                Inactive = 2
            }

            class TestClass
            {
                public Status Status { get; set; }

                public void TestMethod()
                {
                    var value = 1;
                    Status = (Status)value;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EnumPropertyAssignedStringLiteralShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public string Name { get; set; }

                public void TestMethod()
                {
                    Name = "test";
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MemberAccessEnumAssignedLiteralShouldTrigger()
    {
        var source = """
            public enum AccountCategoryCode
            {
                Standard = 1,
                Preferred = 2
            }

            class Account
            {
                public AccountCategoryCode? AccountCategoryCode { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                    account.AccountCategoryCode = 2;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0002", diagnostics[0].Id);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var analyzer = new EnumAssignmentAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0002").ToArray();
    }
}