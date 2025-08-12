using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataverseAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BracesForControlFlowAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        "CT0001",
        Resources.CT0001_Title,
        Resources.CT0001_MessageFormat,
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.CT0001_Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
        context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
        context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
        context.RegisterSyntaxNodeAction(AnalyzeWhileStatement, SyntaxKind.WhileStatement);
        context.RegisterSyntaxNodeAction(AnalyzeDoStatement, SyntaxKind.DoStatement);
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

    private static void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
    {
        var forStatement = (ForStatementSyntax)context.Node;
        AnalyzeEmbeddedStatement(context, forStatement.Statement);
    }

    private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
    {
        var forEachStatement = (ForEachStatementSyntax)context.Node;
        AnalyzeEmbeddedStatement(context, forEachStatement.Statement);
    }

    private static void AnalyzeWhileStatement(SyntaxNodeAnalysisContext context)
    {
        var whileStatement = (WhileStatementSyntax)context.Node;
        AnalyzeEmbeddedStatement(context, whileStatement.Statement);
    }

    private static void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
    {
        var doStatement = (DoStatementSyntax)context.Node;
        AnalyzeEmbeddedStatement(context, doStatement.Statement);
    }

    private static void AnalyzeEmbeddedStatement(SyntaxNodeAnalysisContext context, StatementSyntax statement)
    {
        if (statement is BlockSyntax)
        {
            return;
        }

        if (IsAllowedStatement(statement))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, statement.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsAllowedStatement(StatementSyntax statement)
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