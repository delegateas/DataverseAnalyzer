using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateConstructorParameterTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0005",
        Resources.CT0005_Title,
        Resources.CT0005_MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Resources.CT0005_Description));

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

        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
        context.RegisterSyntaxNodeAction(
            AnalyzePrimaryConstructor,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        if (constructor.ParameterList is null)
            return;

        AnalyzeParameterList(context, constructor.ParameterList);
    }

    private static void AnalyzePrimaryConstructor(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (typeDeclaration.ParameterList is null)
            return;

        AnalyzeParameterList(context, typeDeclaration.ParameterList);
    }

    private static void AnalyzeParameterList(SyntaxNodeAnalysisContext context, ParameterListSyntax parameterList)
    {
        var parameters = parameterList.Parameters;
        if (parameters.Count < 2)
            return;

        var parametersByType = new Dictionary<ITypeSymbol, List<string>>(SymbolEqualityComparer.Default);

        foreach (var parameter in parameters)
        {
            if (parameter.Type is null)
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol is null)
                continue;

            if (!IsDependencyInjectionType(typeSymbol))
                continue;

            var paramName = parameter.Identifier.ValueText;

            if (!parametersByType.TryGetValue(typeSymbol, out var paramNames))
            {
                paramNames = new List<string>();
                parametersByType[typeSymbol] = paramNames;
            }

            paramNames.Add(paramName);
        }

        foreach (var kvp in parametersByType)
        {
            if (kvp.Value.Count < 2)
                continue;

            var typeSymbol = kvp.Key;
            var paramNames = kvp.Value;

            var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var paramNamesJoined = string.Join(", ", paramNames);

            var diagnostic = Diagnostic.Create(
                Rule,
                parameterList.GetLocation(),
                typeName,
                paramNamesJoined);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsDependencyInjectionType(ITypeSymbol typeSymbol)
    {
        var typeName = typeSymbol.Name;
        return typeName.EndsWith("Service", StringComparison.Ordinal) ||
               typeName.EndsWith("Repository", StringComparison.Ordinal) ||
               typeName.EndsWith("Handler", StringComparison.Ordinal) ||
               typeName.EndsWith("Provider", StringComparison.Ordinal) ||
               typeName.EndsWith("Factory", StringComparison.Ordinal) ||
               typeName.EndsWith("Manager", StringComparison.Ordinal) ||
               typeName.EndsWith("Client", StringComparison.Ordinal);
    }
}