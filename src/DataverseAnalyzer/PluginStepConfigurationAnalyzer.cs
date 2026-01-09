using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PluginStepConfigurationAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRuleFilteredAttributesOnCreate = new(() => new DiagnosticDescriptor(
        "CT0008",
        Resources.CT0008_Title,
        Resources.CT0008_MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.CT0008_Description));

    private static readonly Lazy<DiagnosticDescriptor> LazyRulePreImageOnCreate = new(() => new DiagnosticDescriptor(
        "CT0009",
        Resources.CT0009_Title,
        Resources.CT0009_MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.CT0009_Description));

    private static readonly Lazy<DiagnosticDescriptor> LazyRulePostImageOnDelete = new(() => new DiagnosticDescriptor(
        "CT0010",
        Resources.CT0010_Title,
        Resources.CT0010_MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.CT0010_Description));

    public static DiagnosticDescriptor RuleFilteredAttributesOnCreate => LazyRuleFilteredAttributesOnCreate.Value;

    public static DiagnosticDescriptor RulePreImageOnCreate => LazyRulePreImageOnCreate.Value;

    public static DiagnosticDescriptor RulePostImageOnDelete => LazyRulePostImageOnDelete.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        RuleFilteredAttributesOnCreate,
        RulePreImageOnCreate,
        RulePostImageOnDelete);

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

        var methodName = memberAccess.Name.Identifier.ValueText;

        if (methodName == "AddFilteredAttributes")
            AnalyzeAddFilteredAttributes(context, invocation, memberAccess);
        else if (methodName == "AddImage")
            AnalyzeAddImage(context, invocation, memberAccess);
    }

    private static void AnalyzeAddFilteredAttributes(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax memberAccess)
    {
        var operation = FindEventOperation(memberAccess.Expression);

        if (operation == "Create")
        {
            var diagnostic = Diagnostic.Create(
                RuleFilteredAttributesOnCreate,
                invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeAddImage(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax memberAccess)
    {
        var operation = FindEventOperation(memberAccess.Expression);
        var imageType = GetImageTypeFromArguments(invocation);

        if (operation == "Create" && imageType is "PreImage" or "Both")
        {
            var diagnostic = Diagnostic.Create(
                RulePreImageOnCreate,
                invocation.GetLocation(),
                imageType);
            context.ReportDiagnostic(diagnostic);
        }
        else if (operation == "Delete" && imageType is "PostImage" or "Both")
        {
            var diagnostic = Diagnostic.Create(
                RulePostImageOnDelete,
                invocation.GetLocation(),
                imageType);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string? FindEventOperation(ExpressionSyntax expression)
    {
        var current = expression;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax inv)
            {
                var methodName = GetMethodName(inv);
                if (methodName == "RegisterPluginStep")
                    return GetOperationFromRegisterPluginStep(inv);

                current = GetReceiverExpression(inv);
            }
            else if (current is MemberAccessExpressionSyntax ma)
            {
                current = ma.Expression;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name switch
            {
                GenericNameSyntax gns => gns.Identifier.ValueText,
                IdentifierNameSyntax ins => ins.Identifier.ValueText,
                _ => null,
            },
            IdentifierNameSyntax id => id.Identifier.ValueText,
            GenericNameSyntax gn => gn.Identifier.ValueText,
            _ => null,
        };
    }

    private static ExpressionSyntax? GetReceiverExpression(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Expression,
            _ => null,
        };
    }

    private static string? GetOperationFromRegisterPluginStep(InvocationExpressionSyntax invocation)
    {
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            var argText = arg.Expression.ToString();

            if (argText.IndexOf("Create", StringComparison.Ordinal) >= 0)
                return "Create";
            if (argText.IndexOf("Delete", StringComparison.Ordinal) >= 0)
                return "Delete";
            if (argText.IndexOf("Update", StringComparison.Ordinal) >= 0)
                return "Update";
        }

        return null;
    }

    private static string? GetImageTypeFromArguments(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return null;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        var argText = firstArg.ToString();

        if (argText.IndexOf("PreImage", StringComparison.Ordinal) >= 0)
            return "PreImage";
        if (argText.IndexOf("PostImage", StringComparison.Ordinal) >= 0)
            return "PostImage";
        if (argText.IndexOf("Both", StringComparison.Ordinal) >= 0)
            return "Both";

        return null;
    }
}