using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AzureFunctionDocumentationAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0012",
        Resources.CT0012_Title,
        Resources.CT0012_MessageFormat,
        "Documentation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0012_Description));

    public static DiagnosticDescriptor Rule => LazyRule.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (!ContainsAzureFunctionMethod(context, classDeclaration))
            return;

        if (HasValidDocumentation(classDeclaration))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.ValueText);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool ContainsAzureFunctionMethod(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
    {
        foreach (var member in classDeclaration.Members)
        {
            if (member is not MethodDeclarationSyntax method)
                continue;

            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                    if (symbol is null)
                        continue;

                    var containingType = symbol.ContainingType;
                    var namespaceName = containingType.ContainingNamespace?.ToDisplayString();

                    if (namespaceName == "Microsoft.Azure.Functions.Worker" && containingType.Name == "FunctionAttribute")
                        return true;

                    if (namespaceName == "Microsoft.Azure.WebJobs" && containingType.Name == "FunctionNameAttribute")
                        return true;
                }
            }
        }

        return false;
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
                return true;
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

            foreach (var contentNode in element.Content)
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