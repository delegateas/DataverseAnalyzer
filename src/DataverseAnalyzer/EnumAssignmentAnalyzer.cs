using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumAssignmentAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0002",
        Resources.CT0002_Title,
        Resources.CT0002_MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0002_Description));

    public static DiagnosticDescriptor Rule => LazyRule.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignmentExpression, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        AnalyzeEnumAssignment(context, assignment.Left, assignment.Right);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var property = (PropertyDeclarationSyntax)context.Node;
        if (property.Initializer is not null)
        {
            AnalyzeEnumAssignmentForProperty(context, property, property.Initializer.Value);
        }
    }

    private static void AnalyzeEnumAssignmentForProperty(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax property, ExpressionSyntax right)
    {
        // Check if the right side is a numeric literal
        if (right is not LiteralExpressionSyntax literal || !IsNumericLiteral(literal))
        {
            return;
        }

        // Get the type of the property from its declaration
        var propertyTypeInfo = context.SemanticModel.GetTypeInfo(property.Type);
        var targetType = propertyTypeInfo.Type;

        // Handle nullable enum types
        if (targetType is null)
        {
            return;
        }

        if (targetType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            targetType = ((INamedTypeSymbol)targetType).TypeArguments[0];
        }

        // Check if the target type is an enum
        if (targetType.TypeKind != TypeKind.Enum)
        {
            return;
        }

        var targetName = property.Identifier.ValueText;
        var literalValue = literal.Token.ValueText;

        var diagnostic = Diagnostic.Create(Rule, right.GetLocation(), targetName, literalValue);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeEnumAssignment(SyntaxNodeAnalysisContext context, SyntaxNode left, ExpressionSyntax right)
    {
        // Check if the right side is a numeric literal
        if (right is not LiteralExpressionSyntax literal || !IsNumericLiteral(literal))
        {
            return;
        }

        // Get the type of the left side
        var leftTypeInfo = context.SemanticModel.GetTypeInfo(left);
        var targetType = leftTypeInfo.Type;

        // Handle nullable enum types
        if (targetType is null)
        {
            return;
        }

        if (targetType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            targetType = ((INamedTypeSymbol)targetType).TypeArguments[0];
        }

        // Check if the target type is an enum
        if (targetType.TypeKind != TypeKind.Enum)
        {
            return;
        }

        var targetName = GetTargetName(left);
        var literalValue = literal.Token.ValueText;

        var diagnostic = Diagnostic.Create(Rule, right.GetLocation(), targetName, literalValue);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsNumericLiteral(LiteralExpressionSyntax literal)
    {
        return literal.Token.IsKind(SyntaxKind.NumericLiteralToken);
    }

    private static string GetTargetName(SyntaxNode left)
    {
        return left switch
        {
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => "property",
        };
    }
}