using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObjectInitializationAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0003",
        Resources.CT0003_Title,
        Resources.CT0003_MessageFormat,
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0003_Description));

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
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // Check if it has an object initializer
        if (objectCreation.Initializer is null)
        {
            return;
        }

        // Check if it has an argument list (parentheses)
        if (objectCreation.ArgumentList is null)
        {
            return;
        }

        // Check if the argument list is empty (no arguments between parentheses)
        if (objectCreation.ArgumentList.Arguments.Count > 0)
        {
            return;
        }

        // Get the type name for the diagnostic message
        var typeName = objectCreation.Type?.ToString() ?? "object";

        var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation(), typeName);
        context.ReportDiagnostic(diagnostic);
    }
}