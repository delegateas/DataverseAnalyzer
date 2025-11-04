using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoBracesForControlFlowAnalyzer : DiagnosticAnalyzer
{
    private static readonly Lazy<DiagnosticDescriptor> LazyRule = new(() => new DiagnosticDescriptor(
        "CT0004",
        Resources.CT0004_Title,
        Resources.CT0004_MessageFormat,
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.CT0004_Description));

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
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;
        AnalyzeEmbeddedStatement(context, ifStatement.Statement);
    }

    private static void AnalyzeElseClause(SyntaxNodeAnalysisContext context)
    {
        var elseClause = (ElseClauseSyntax)context.Node;
        if (elseClause.Statement is not IfStatementSyntax)
        {
            AnalyzeEmbeddedStatement(context, elseClause.Statement);
        }
    }

    private static void AnalyzeEmbeddedStatement(SyntaxNodeAnalysisContext context, StatementSyntax statement)
    {
        if (statement is not BlockSyntax block)
        {
            return;
        }

        if (block.Statements.Count != 1)
        {
            return;
        }

        var singleStatement = block.Statements[0];
        if (!IsControlFlowStatement(singleStatement))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, block.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsControlFlowStatement(StatementSyntax statement)
    {
        return statement.Kind() switch
        {
            SyntaxKind.ReturnStatement => true,
            SyntaxKind.ThrowStatement => true,
            SyntaxKind.ContinueStatement => true,
            SyntaxKind.BreakStatement => true,
            SyntaxKind.YieldReturnStatement => false,
            SyntaxKind.YieldBreakStatement => true,
            _ => false,
        };
    }
}