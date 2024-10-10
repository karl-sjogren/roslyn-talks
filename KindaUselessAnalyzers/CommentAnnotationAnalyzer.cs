using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KindaUselessAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentAnnotationAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_diagnosticDescriptor];

    private DiagnosticDescriptor _diagnosticDescriptor { get; } = new(
            id: "XA0003",
            title: "Comments should have a date and signature.",
            messageFormat: "All comments should start with a date and signature in the format 'YYYY-MM-DD NN'.",
            category: "XLENT",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(CheckSingleLineComment);
    }

    private static readonly Regex _regex = new(@"^\d{4}-\d{2}-\d{2} .{2,5}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private void CheckSingleLineComment(SyntaxTreeAnalysisContext context) {
        var root = context.Tree.GetRoot();

        var singleLineCommentTrivia = root
            .DescendantTrivia()
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .ToArray();

        foreach(var comment in singleLineCommentTrivia) {
            var currentCommentLine = comment.GetLocation().GetLineSpan().StartLinePosition.Line;

            var hasPrecedingSingleLineComment = singleLineCommentTrivia
                .Any(trivia => trivia.GetLocation().GetLineSpan().StartLinePosition.Line == currentCommentLine - 1);

            if(hasPrecedingSingleLineComment) {
                continue;
            }

            var commentText = comment.ToString().Trim();

            if(commentText.StartsWith("// ")) {
                commentText = commentText.Substring(3).Trim();
            }

            if(string.IsNullOrWhiteSpace(commentText)) {
                continue;
            }

            CheckComment(context, comment, commentText);
        }

        var multiLineCommentTrivia = root
            .DescendantTrivia()
            .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            .ToArray();

        foreach(var comment in multiLineCommentTrivia) {
            var commentText = comment.ToString().Trim();

            if(commentText.StartsWith("/*")) {
                commentText = commentText.Substring(2).Trim();
            }

            if(commentText.EndsWith("*/")) {
                commentText = commentText.Substring(0, commentText.Length - 2).Trim();
            }

            if(string.IsNullOrWhiteSpace(commentText)) {
                continue;
            }

            CheckComment(context, comment, commentText);
        }
    }

    private void CheckComment(SyntaxTreeAnalysisContext context, SyntaxTrivia comment, string commentText) {
        if(_regex.IsMatch(commentText)) {
            return;
        }

        var diagnostic = Diagnostic.Create(
            descriptor: _diagnosticDescriptor,
            location: comment.GetLocation(),
            messageArgs: [commentText]);

        context.ReportDiagnostic(diagnostic);
    }
}
