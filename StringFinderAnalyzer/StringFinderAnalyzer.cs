using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringFinderAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringFinderAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ _diagnosticDescriptor ];

    private DiagnosticDescriptor _diagnosticDescriptor { get; } = new(
            id: "XA0001",
            title: "This is a string",
            messageFormat: "Move a long, nothing to see here. Just a string: {0}",
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
        if (context.Node is not LiteralExpressionSyntax literalNode) {
            return;
        }

        var diagnostic = Diagnostic.Create(
            descriptor: _diagnosticDescriptor,
            location: literalNode.GetLocation(),
            messageArgs: [literalNode.Token.Text]);

        context.ReportDiagnostic(diagnostic);
    }
}