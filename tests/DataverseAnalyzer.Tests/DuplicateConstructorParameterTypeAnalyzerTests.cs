using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class DuplicateConstructorParameterTypeAnalyzerTests
{
    [Fact]
    public async Task RegularConstructorWithDuplicateServiceTypeShouldTrigger()
    {
        var source = """
            interface IMyService { }

            class TestService
            {
                public TestService(IMyService a, IMyService b)
                {
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task PrimaryConstructorWithDuplicateServiceTypeShouldTrigger()
    {
        var source = """
            interface IMyService { }

            class TestService(IMyService a, IMyService b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task RecordWithDuplicateServiceTypeShouldTrigger()
    {
        var source = """
            class MyService { }

            record TestRecord(MyService A, MyService B);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task ThreeSameServiceTypesShouldTriggerOnce()
    {
        var source = """
            interface IMyService { }

            class TestService(IMyService a, IMyService b, IMyService c);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task TwoDifferentDuplicateServiceTypesShouldTriggerTwice()
    {
        var source = """
            interface IUserService { }
            interface IOrderService { }

            class TestClass(IUserService a1, IUserService a2, IOrderService b1, IOrderService b2);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0005", d.Id));
    }

    [Fact]
    public async Task UniqueServiceTypesShouldNotTrigger()
    {
        var source = """
            interface IUserService { }
            interface IOrderService { }
            interface IProductService { }

            class TestClass(IUserService a, IOrderService b, IProductService c);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SingleParameterShouldNotTrigger()
    {
        var source = """
            interface IMyService { }

            class TestService(IMyService a);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EmptyConstructorShouldNotTrigger()
    {
        var source = """
            class TestService
            {
                public TestService()
                {
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DifferentGenericTypeArgumentsShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestService(List<string> a, List<int> b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicateCollectionTypesShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestService(List<string> a, List<string> b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicateNonDiCustomTypesShouldNotTrigger()
    {
        var source = """
            class MyDto { }

            class TestClass(MyDto a, MyDto b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicateRepositoryTypeShouldTrigger()
    {
        var source = """
            class UserRepository { }

            class TestClass(UserRepository a, UserRepository b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task DuplicateHandlerTypeShouldTrigger()
    {
        var source = """
            class CommandHandler { }

            class TestClass(CommandHandler a, CommandHandler b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task DuplicateClientTypeShouldTrigger()
    {
        var source = """
            class HttpClient { }

            class TestClass(HttpClient a, HttpClient b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task InterfaceAndImplementationShouldNotTrigger()
    {
        var source = """
            interface IUserService { }
            class UserService : IUserService { }

            class TestClass(IUserService a, UserService b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonDiInterfaceShouldNotTrigger()
    {
        var source = """
            interface IComparable { }

            class TestClass(IComparable a, IComparable b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StructWithDuplicateServiceTypeShouldTrigger()
    {
        var source = """
            interface IMyService { }

            struct TestStruct(IMyService a, IMyService b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task RecordStructWithDuplicateServiceTypeShouldTrigger()
    {
        var source = """
            interface IMyService { }

            record struct TestRecordStruct(IMyService A, IMyService B);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task DuplicatePrimitiveStringShouldNotTrigger()
    {
        var source = """
            class TestService(string firstName, string lastName);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicatePrimitiveIntShouldNotTrigger()
    {
        var source = """
            class TestService(int width, int height);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicatePrimitiveBoolShouldNotTrigger()
    {
        var source = """
            class TestService(bool isEnabled, bool isVisible);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MixedPrimitivesAndServicesShouldOnlyTriggerForServices()
    {
        var source = """
            interface IMyService { }

            class TestService(string name, IMyService a, int count, IMyService b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0005", diagnostics[0].Id);
    }

    [Fact]
    public async Task DuplicateDictionaryTypesShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestService(Dictionary<string, int> a, Dictionary<string, int> b);
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DuplicateIEnumerableTypesShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestService(IEnumerable<string> a, IEnumerable<string> b);
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

        var analyzer = new DuplicateConstructorParameterTypeAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0005").ToArray();
    }
}