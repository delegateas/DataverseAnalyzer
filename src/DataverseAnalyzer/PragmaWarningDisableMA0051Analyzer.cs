using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PragmaWarningDisableMA0051Analyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0011",
        Resources.CT0011_Title,
        Resources.CT0011_MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0011_Description));

    public static DiagnosticDescriptor Rule => LazyRule.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzePragmaWarning, SyntaxKind.PragmaWarningDirectiveTrivia);
    }

    private static void AnalyzePragmaWarning(SyntaxNodeAnalysisContext context)
    {
        var pragma = (PragmaWarningDirectiveTriviaSyntax)context.Node;

        if (!pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
            return;

        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (errorCode is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "MA0051")
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, pragma.GetLocation()));
                return;
            }
        }
    }
}