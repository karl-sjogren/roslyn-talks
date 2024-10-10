using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace KindaUselessAnalyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringWithNonUniformCasingCodeFixProvider)), Shared]
public sealed class StringWithNonUniformCasingCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => ["XA0002"];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync();
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var literalSyntax = root.FindToken(diagnosticSpan.Start);
        if (literalSyntax == default)
            return;

        string ToUpper(string text) => text.ToUpper();

        var makeStringUpperCodeAction = CodeAction.Create(
            "Make string upper case",
            cancellationToken => RefactorAsync(context.Document, literalSyntax, ToUpper, cancellationToken),
            equivalenceKey: $"{nameof(StringWithNonUniformCasingCodeFixProvider)}_ToUpper");

        context.RegisterCodeFix(makeStringUpperCodeAction, context.Diagnostics);

        string ToLower(string text) => text.ToLower();

        var makeStringLowerCodeAction = CodeAction.Create(
            "Make string lower case",
            cancellationToken => RefactorAsync(context.Document, literalSyntax, ToLower, cancellationToken),
            equivalenceKey: $"{nameof(StringWithNonUniformCasingCodeFixProvider)}_ToLower");

        context.RegisterCodeFix(makeStringLowerCodeAction, context.Diagnostics);
    }

    public static async Task<Solution> RefactorAsync(
            Document document,
            SyntaxToken literalSyntax,
            Func<string, string> convert,
            CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) {
            throw new InvalidOperationException("Could not get syntax root");
        }

        var newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken(convert(literalSyntax.Text)));
        var newRoot = root.ReplaceNode(literalSyntax.Parent!, newLiteral);

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }
}
