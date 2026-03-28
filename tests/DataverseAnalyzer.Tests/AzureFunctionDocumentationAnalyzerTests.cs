using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class AzureFunctionDocumentationAnalyzerTests
{
    private const string FunctionAttributeDefinition = """
        namespace Microsoft.Azure.Functions.Worker
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class FunctionAttribute : System.Attribute
            {
                public FunctionAttribute(string name) { }
            }
        }
        """;

    private const string FunctionNameAttributeDefinition = """
        namespace Microsoft.Azure.WebJobs
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class FunctionNameAttribute : System.Attribute
            {
                public FunctionNameAttribute(string name) { }
            }
        }
        """;

    [Fact]
    public async Task AzureFunctionClassWithoutXmlCommentShouldTrigger()
    {
        var source = FunctionAttributeDefinition + """

            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task AzureFunctionClassWithEmptySummaryShouldTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <summary></summary>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task AzureFunctionClassWithWhitespaceSummaryShouldTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <summary>   </summary>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task AzureFunctionClassWithValidSummaryShouldNotTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <summary>Handles work item processing.</summary>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AzureFunctionClassWithMultiLineSummaryShouldNotTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <summary>
            /// Handles work item processing and validation.
            /// </summary>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AzureFunctionClassWithInheritdocShouldNotTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <inheritdoc/>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AzureFunctionClassWithInheritdocCrefShouldNotTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <inheritdoc cref="object"/>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassWithNoFunctionMethodsShouldNotTrigger()
    {
        var source = """
            class MyClass
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassWithOnlyRemarksAndFunctionMethodShouldTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <remarks>Some remarks here.</remarks>
            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task InProcessFunctionNameAttributeWithoutSummaryShouldTrigger()
    {
        var source = FunctionNameAttributeDefinition + """

            class MyFunctions
            {
                [Microsoft.Azure.WebJobs.FunctionName("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task InProcessFunctionNameAttributeWithValidSummaryShouldNotTrigger()
    {
        var source = FunctionNameAttributeDefinition + """

            /// <summary>Handles in-process work.</summary>
            class MyFunctions
            {
                [Microsoft.Azure.WebJobs.FunctionName("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassWithMultipleFunctionMethodsWithoutSummaryShouldTriggerOnce()
    {
        var source = FunctionAttributeDefinition + """

            class MyFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }

                [Microsoft.Azure.Functions.Worker.Function("DoMoreWork")]
                public void DoMoreWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0012", diagnostics[0].Id);
    }

    [Fact]
    public async Task MultipleClassesWithMixedDocsShouldTriggerOnlyForMissing()
    {
        var source = FunctionAttributeDefinition + """

            class UndocumentedFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoWork")]
                public void DoWork() { }
            }

            /// <summary>Documented function class.</summary>
            class DocumentedFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoOtherWork")]
                public void DoOtherWork() { }
            }

            class AnotherUndocumentedFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("DoMoreWork")]
                public void DoMoreWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0012", d.Id));
    }

    [Fact]
    public async Task DiagnosticContainsClassName()
    {
        var source = FunctionAttributeDefinition + """

            class OrderProcessingFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("ProcessOrder")]
                public void ProcessOrder() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("OrderProcessingFunctions", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClassWithFunctionAttributeFromWrongNamespaceShouldNotTrigger()
    {
        var source = """
            namespace SomeOtherNamespace
            {
                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class FunctionAttribute : System.Attribute
                {
                    public FunctionAttribute(string name) { }
                }
            }

            class MyFunctions
            {
                [SomeOtherNamespace.Function("DoWork")]
                public void DoWork() { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AzureFunctionClassWithSummaryAndOtherTagsShouldNotTrigger()
    {
        var source = FunctionAttributeDefinition + """

            /// <summary>Handles order processing.</summary>
            /// <remarks>Additional details here.</remarks>
            class OrderFunctions
            {
                [Microsoft.Azure.Functions.Worker.Function("ProcessOrder")]
                public void ProcessOrder() { }
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

        var analyzer = new AzureFunctionDocumentationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0012").ToArray();
    }
}
