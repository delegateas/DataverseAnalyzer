using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataverseAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoBracesForControlFlowCodeFixProvider))]
public sealed class NoBracesForControlFlowCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NoBracesForControlFlowAnalyzer.Rule.Id);

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
        var block = root.FindNode(diagnosticSpan) as BlockSyntax;
        if (block is null || block.Statements.Count != 1)
        {
            return;
        }

        var action = CodeAction.Create(
            title: Resources.CT0004_CodeFix_Title,
            createChangedDocument: c => RemoveBraces(context.Document, root, block, c),
            equivalenceKey: "RemoveBraces");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static Task<Document> RemoveBraces(Document document, SyntaxNode root, BlockSyntax block, CancellationToken cancellationToken)
    {
        var statement = block.Statements[0];
        var newStatement = statement.WithTriviaFrom(block);

        var newRoot = root.ReplaceNode(block, newStatement);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return Task.FromResult(newDocument);
    }
}