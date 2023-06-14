using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stl.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandHandlerAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DirectCallDiagnostic =
        new(
            "STL0001",
            "Invalid command handler call",
            "Direct command handler calls on command service proxies are not allowed",
            "Test",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DirectCallDiagnostic);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(CheckCommandHandler, SyntaxKind.InvocationExpression);
    }

    private void CheckCommandHandler(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation, cancellationToken: context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();

        if (syntaxReference is null)
            return;

        SyntaxNode declaration = syntaxReference.GetSyntax(context.CancellationToken);

        if (declaration is not MethodDeclarationSyntax method)
            return;

        // check if the called method has the CommandHandler attribute
        // this is a very naive implementation, the namespace should be checked as well
        bool isCommandHandler = method.AttributeLists
            .SelectMany(x => x.Attributes)
            .Any(
                attribute =>
                    string.Equals(
                        attribute.Name.ToString(),
                        "CommandHandler",
                        StringComparison.Ordinal
                    )
            );

        if (!isCommandHandler)
            return;

        var diagnostic = Diagnostic.Create(DirectCallDiagnostic, context.Node.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
