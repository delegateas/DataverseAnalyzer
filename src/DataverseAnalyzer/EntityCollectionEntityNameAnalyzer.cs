using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityCollectionEntityNameAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0013",
        Resources.CT0013_Title,
        Resources.CT0013_MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0013_Description));

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

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
        if (!IsEntityCollection(typeInfo.Type))
            return;

        if (HasConstructorArguments(objectCreation))
            return;

        if (HasEntityNameInInitializer(objectCreation))
            return;

        var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsEntityCollection(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return type.Name == "EntityCollection" &&
               type.ContainingNamespace?.ToDisplayString() == "Microsoft.Xrm.Sdk";
    }

    private static bool HasConstructorArguments(ObjectCreationExpressionSyntax objectCreation)
    {
        return objectCreation.ArgumentList is not null &&
               objectCreation.ArgumentList.Arguments.Count > 0;
    }

    private static bool HasEntityNameInInitializer(ObjectCreationExpressionSyntax objectCreation)
    {
        if (objectCreation.Initializer is null)
            return false;

        foreach (var expression in objectCreation.Initializer.Expressions)
        {
            if (expression is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "EntityName")
                return true;
        }

        return false;
    }
}
