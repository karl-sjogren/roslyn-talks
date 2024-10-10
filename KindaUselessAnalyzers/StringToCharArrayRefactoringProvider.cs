using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KindaUselessAnalyzers;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StringToCharArrayRefactoringProvider))]
public class StringToCharArrayRefactoringProvider : CodeRefactoringProvider {
    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
        var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if(rootNode is null) {
            return;
        }

        var literalExpression = rootNode.FindNode(context.Span).FirstAncestorOrSelf<LiteralExpressionSyntax>();

        if(literalExpression is null) {
            return;
        }

        var action = CodeAction.Create("Change to char array", codeAnalysisProgress => ChangeToCharArray(context.Document, literalExpression, codeAnalysisProgress));
        context.RegisterRefactoring(action);
    }

    private async Task<Document> ChangeToCharArray(Document document, LiteralExpressionSyntax node, CancellationToken cancellationToken) {
        var solution = document.Project.Solution;
        var syntaxTree = node.SyntaxTree;

        var tokenText = node.Token.ValueText;
        var updatedNode = SyntaxFactory.ParseExpression($"string.Join({string.Join(", ", tokenText.Select(c => $"'{c}'"))})");

        var rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if(rootNode is null) {
            return document;
        }

        rootNode = rootNode.ReplaceNode(node, updatedNode);
        return document.WithSyntaxRoot(rootNode);
    }
}
