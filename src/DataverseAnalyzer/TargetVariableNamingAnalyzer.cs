using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetVariableNamingAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRuleGetTarget = new(() => new DiagnosticDescriptor(
        "CT0011",
        Resources.CT0011_Title,
        Resources.CT0011_MessageFormat,
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0011_Description));

    private static readonly Lazy<DiagnosticDescriptor> LazyRuleMergedWithPreImage = new(() => new DiagnosticDescriptor(
        "CT0012",
        Resources.CT0012_Title,
        Resources.CT0012_MessageFormat,
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0012_Description));

    public static DiagnosticDescriptor RuleGetTarget => LazyRuleGetTarget.Value;

    public static DiagnosticDescriptor RuleMergedWithPreImage => LazyRuleMergedWithPreImage.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        RuleGetTarget,
        RuleMergedWithPreImage);

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

        foreach (var variable in localDeclaration.Declaration.Variables)
        {
            var initializer = variable.Initializer?.Value;
            if (initializer is null)
                continue;

            var invocation = ExtractInvocation(initializer);
            if (invocation is null)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var methodName = GetMethodName(memberAccess);
            var variableName = variable.Identifier.ValueText;

            if (methodName == "GetTarget" && variableName != "target")
            {
                var diagnostic = Diagnostic.Create(
                    RuleGetTarget,
                    variable.Identifier.GetLocation(),
                    variableName);
                context.ReportDiagnostic(diagnostic);
            }
            else if (methodName == "GetTargetMergedWithPreImage" && variableName != "merged")
            {
                var diagnostic = Diagnostic.Create(
                    RuleMergedWithPreImage,
                    variable.Identifier.GetLocation(),
                    variableName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static InvocationExpressionSyntax? ExtractInvocation(ExpressionSyntax expression)
    {
        if (expression is AwaitExpressionSyntax awaitExpr)
            expression = awaitExpr.Expression;

        while (expression is CastExpressionSyntax castExpr)
            expression = castExpr.Expression;

        while (expression is ParenthesizedExpressionSyntax parenExpr)
            expression = parenExpr.Expression;

        return expression as InvocationExpressionSyntax;
    }

    private static string? GetMethodName(MemberAccessExpressionSyntax memberAccess)
    {
        return memberAccess.Name switch
        {
            GenericNameSyntax genericName => genericName.Identifier.ValueText,
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            _ => null,
        };
    }
}
