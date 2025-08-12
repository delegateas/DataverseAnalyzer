using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataverseAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BracesForControlFlowCodeFixProvider))]
public sealed class BracesForControlFlowCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BracesForControlFlowAnalyzer.Rule.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.FirstOrDefault(d => FixableDiagnosticIds.Contains(d.Id, StringComparer.Ordinal));
        if (diagnostic is null)
        {
            return;
        }

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var statement = root.FindNode(diagnosticSpan) as StatementSyntax;
        if (statement is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: Resources.CT0001_CodeFix_Title,
            createChangedDocument: c => AddBraces(context.Document, root, statement, c),
            equivalenceKey: "AddBraces");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static Task<Document> AddBraces(Document document, SyntaxNode root, StatementSyntax statement, CancellationToken cancellationToken)
    {
        var blockStatement = SyntaxFactory.Block(statement)
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
            .WithTriviaFrom(statement);

        var newRoot = root.ReplaceNode(statement, blockStatement);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return Task.FromResult(newDocument);
    }
}