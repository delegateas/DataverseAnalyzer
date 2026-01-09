using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer.Tests;

public sealed class EntityContainsAnalyzerTests
{
    [Fact]
    public async Task DirectEntityContainsWithStringShouldTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var result = entity.Contains("accountname");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0007", diagnostics[0].Id);
    }

    [Fact]
    public async Task DerivedEntityContainsWithStringShouldTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class Account : Microsoft.Xrm.Sdk.Entity { }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                    var result = account.Contains("accountname");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0007", diagnostics[0].Id);
    }

    [Fact]
    public async Task DeepInheritanceChainContainsShouldTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class CustomEntity : Microsoft.Xrm.Sdk.Entity { }
            class Account : CustomEntity { }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                    var result = account.Contains("name");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0007", diagnostics[0].Id);
    }

    [Fact]
    public async Task ContainsInIfConditionShouldTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    if (entity.Contains("attribute"))
                    {
                    }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0007", diagnostics[0].Id);
    }

    [Fact]
    public async Task ContainsWithStringVariableShouldTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var attrName = "accountname";
                    var result = entity.Contains(attrName);
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Equal("CT0007", diagnostics[0].Id);
    }

    [Fact]
    public async Task MultipleContainsCallsShouldTriggerMultipleTimes()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var a = entity.Contains("attr1");
                    var b = entity.Contains("attr2");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("CT0007", d.Id));
    }

    [Fact]
    public async Task DiagnosticMessageContainsAttributeName()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool Contains(string attributeName) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var result = entity.Contains("myattribute");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Single(diagnostics);
        Assert.Contains("myattribute", diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task IndexerAccessShouldNotTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public object this[string attributeName] => null;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var value = entity["accountname"];
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GetAttributeValueShouldNotTrigger()
    {
        var source = """
            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public T GetAttributeValue<T>(string attributeName) => default;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new Microsoft.Xrm.Sdk.Entity();
                    var value = entity.GetAttributeValue<string>("accountname");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ContainsAttributesShouldNotTrigger()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            namespace Microsoft.Xrm.Sdk
            {
                public class Entity
                {
                    public bool ContainsAttributes<T>(params Expression<Func<T, object>>[] selectors) => true;
                }
            }

            class Account : Microsoft.Xrm.Sdk.Entity
            {
                public string Name { get; set; }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var account = new Account();
                    var result = account.ContainsAttributes<Account>(x => x.Name);
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonEntityContainsShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                public void TestMethod()
                {
                    var list = new List<string> { "a", "b" };
                    var result = list.Contains("a");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StringContainsShouldNotTrigger()
    {
        var source = """
            class TestClass
            {
                public void TestMethod()
                {
                    var text = "hello world";
                    var result = text.Contains("hello");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DictionaryContainsKeyShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                public void TestMethod()
                {
                    var dict = new Dictionary<string, object>();
                    var result = dict.ContainsKey("key");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonXrmEntityContainsShouldNotTrigger()
    {
        var source = """
            namespace OtherNamespace
            {
                public class Entity
                {
                    public bool Contains(string name) => true;
                }
            }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new OtherNamespace.Entity();
                    var result = entity.Contains("test");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EntityWithDifferentNamespaceButSameNameShouldNotTrigger()
    {
        var source = """
            namespace MyCompany.Data
            {
                public class Entity
                {
                    public bool Contains(string name) => true;
                }
            }

            class MyEntity : MyCompany.Data.Entity { }

            class TestClass
            {
                public void TestMethod()
                {
                    var entity = new MyEntity();
                    var result = entity.Contains("test");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HashSetContainsShouldNotTrigger()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                public void TestMethod()
                {
                    var set = new HashSet<string>();
                    var result = set.Contains("item");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var expressionAssembly = typeof(System.Linq.Expressions.Expression).Assembly.Location;
        if (!string.IsNullOrEmpty(expressionAssembly))
        {
            references.Add(MetadataReference.CreateFromFile(expressionAssembly));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new EntityContainsAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "CT0007").ToArray();
    }
}
