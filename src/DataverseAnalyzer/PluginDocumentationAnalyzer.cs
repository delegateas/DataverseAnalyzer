using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PluginDocumentationAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0006",
        Resources.CT0006_Title,
        Resources.CT0006_MessageFormat,
        "Documentation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0006_Description));

    public static DiagnosticDescriptor Rule => LazyRule.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (!InheritsFromPlugin(classDeclaration))
            return;

        if (HasValidDocumentation(classDeclaration))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.ValueText);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool InheritsFromPlugin(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.BaseList is null)
            return false;

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var typeName = GetBaseTypeName(baseType.Type);
            if (typeName == "Plugin")
                return true;
        }

        return false;
    }

    private static string? GetBaseTypeName(TypeSyntax type)
    {
        return type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => null,
        };
    }

    private static bool HasValidDocumentation(ClassDeclarationSyntax classDeclaration)
    {
        var leadingTrivia = classDeclaration.GetLeadingTrivia();

        foreach (var trivia in leadingTrivia)
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                continue;

            var structure = trivia.GetStructure();
            if (structure is null)
                continue;

            if (HasInheritdoc(structure))
                return true;

            if (HasNonEmptySummary(structure))
                return true;
        }

        return false;
    }

    private static bool HasInheritdoc(SyntaxNode structure)
    {
        foreach (var node in structure.DescendantNodes())
        {
            if (node is XmlEmptyElementSyntax emptyElement &&
                emptyElement.Name.LocalName.ValueText == "inheritdoc")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNonEmptySummary(SyntaxNode structure)
    {
        foreach (var node in structure.DescendantNodes())
        {
            if (node is not XmlElementSyntax element)
                continue;

            if (element.StartTag.Name.LocalName.ValueText != "summary")
                continue;

            var content = element.Content;
            foreach (var contentNode in content)
            {
                if (contentNode is XmlTextSyntax textSyntax)
                {
                    var text = string.Concat(textSyntax.TextTokens.Select(t => t.ValueText));
                    if (!string.IsNullOrWhiteSpace(text))
                        return true;
                }
            }
        }

        return false;
    }
}