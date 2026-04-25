using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class EntityCollectionEntityNameAnalyzerTests
{
    private const string XrmSdkDefinition = """
        namespace Microsoft.Xrm.Sdk
        {
            public class Entity { }

            public class EntityCollection
            {
                public EntityCollection() { }
                public EntityCollection(string entityName) { EntityName = entityName; }
                public string EntityName { get; set; }
            }
        }
        """;

    [Fact]
    public async Task ParameterlessConstructorWithoutInitializerShouldTrigger()
    {
        var source = XrmSdkDefinition + """

            class TestClass
            {
                public void TestMethod()
                {
                    var collection = new Microsoft.Xrm.Sdk.EntityCollection();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0013", diagnostics[0].Id);
    }

    [Fact]
    public async Task ParameterlessConstructorWithEmptyInitializerShouldTrigger()
    {
        var source = XrmSdkDefinition + """

            class TestClass
            {
                public void TestMethod()
                {
                    var collection = new Microsoft.Xrm.Sdk.EntityCollection { };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0013", diagnostics[0].Id);
    }

    [Fact]
    public async Task ParameterlessConstructorWithEntityNameInInitializerShouldNotTrigger()
    {
        var source = XrmSdkDefinition + """

            class TestClass
            {
                public void TestMethod()
                {
                    var collection = new Microsoft.Xrm.Sdk.EntityCollection { EntityName = "account" };
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConstructorWithEntityNameArgumentShouldNotTrigger()
    {
        var source = XrmSdkDefinition + """

            class TestClass
            {
                public void TestMethod()
                {
                    var collection = new Microsoft.Xrm.Sdk.EntityCollection("account");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonXrmEntityCollectionShouldNotTrigger()
    {
        var source = """
            namespace OtherNamespace
            {
                public class EntityCollection
                {
                    public EntityCollection() { }
                    public string EntityName { get; set; }
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var collection = new OtherNamespace.EntityCollection();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleCreationsShouldTriggerForEachViolation()
    {
        var source = XrmSdkDefinition + """

            class TestClass
            {
                public void TestMethod()
                {
                    var a = new Microsoft.Xrm.Sdk.EntityCollection();
                    var b = new Microsoft.Xrm.Sdk.EntityCollection();
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0013", d.Id));
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new EntityCollectionEntityNameAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0013").ToArray();
    }
}
