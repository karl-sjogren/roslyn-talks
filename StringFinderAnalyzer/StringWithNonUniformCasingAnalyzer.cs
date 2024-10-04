using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringFinderAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringWithNonUniformCasingAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ _diagnosticDescriptor ];

    private DiagnosticDescriptor _diagnosticDescriptor { get; } = new(
            id: "XA0002",
            title: "This string is not completely upper case or lower case",
            messageFormat: "Strings looks best when not mixing upper case and lower case.",
            category: "XLENT",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(CheckStringLiteralTokens, SyntaxKind.StringLiteralExpression);
    }

    private void CheckStringLiteralTokens(SyntaxNodeAnalysisContext context) {
        if(context.Node is not LiteralExpressionSyntax literalNode) {
            return;
        }

        var text = literalNode.Token.Text;
        if(text == text.ToUpper() || text == text.ToLower()) {
            return;
        }

        var diagnostic = Diagnostic.Create(
            descriptor: _diagnosticDescriptor,
            location: literalNode.GetLocation(),
            messageArgs: [text]);

        context.ReportDiagnostic(diagnostic);
    }
}