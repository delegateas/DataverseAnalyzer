using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityContainsAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0007",
        Resources.CT0007_Title,
        Resources.CT0007_MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0007_Description));

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

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.ValueText != "Contains")
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!IsStringContainsMethod(methodSymbol))
            return;

        var receiverTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
        var receiverType = receiverTypeInfo.Type;

        if (!InheritsFromEntity(receiverType))
            return;

        var attributeName = GetAttributeNameFromArgument(invocation);
        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            attributeName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsStringContainsMethod(IMethodSymbol method)
    {
        return method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static bool InheritsFromEntity(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        var current = type;
        while (current is not null)
        {
            var namespaceName = current.ContainingNamespace?.ToDisplayString();
            if (namespaceName == "Microsoft.Xrm.Sdk" && current.Name == "Entity")
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static string GetAttributeNameFromArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 1)
        {
            var argument = invocation.ArgumentList.Arguments[0].Expression;
            if (argument is LiteralExpressionSyntax literal)
                return literal.Token.ValueText;
        }

        return "attribute";
    }
}