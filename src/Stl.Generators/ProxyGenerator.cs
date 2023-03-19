namespace Stl.Generators;
using static DiagnosticsHelpers;
using static GenerationHelpers;

[Generator]
public class ProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var items = context.SyntaxProvider
            .CreateSyntaxProvider(CouldBeAugmented, MustAugment)
            .Where(i => i.TypeDef != null)
            .Collect();
        context.RegisterSourceOutput(items, Generate);
    }

    private bool CouldBeAugmented(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax or InterfaceDeclarationSyntax {
            Parent: FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax
        };

    private (SemanticModel SemanticModel, TypeDeclarationSyntax? TypeDef)
        MustAugment(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;
        var typeDef = (TypeDeclarationSyntax) context.Node;
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDef);
        if (typeSymbol == null || typeSymbol.IsSealed || !typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
            return default;
        if (typeDef is ClassDeclarationSyntax && typeSymbol.IsAbstract)
            return default;

        var requiresProxy = typeSymbol.AllInterfaces.Any(t => Equals(t.ToFullName(), RequireAsyncProxyInterfaceName));
        return requiresProxy ? (semanticModel, typeDef) : default;
    }

    private void Generate(
        SourceProductionContext context,
        ImmutableArray<(SemanticModel SemanticModel, TypeDeclarationSyntax? TypeDef)> items)
    {
        if (items.Length == 0)
            return;
        try {
            WriteDebug?.Invoke($"Found {items.Length} type(s) to generate proxies.");
            foreach (var (semanticModel, typeDef) in items) {
                if (typeDef == null)
                    continue;

                var typeGenerator = new ProxyTypeGenerator(context, semanticModel, typeDef);
                var code = typeGenerator.GeneratedCode;
                if (string.IsNullOrEmpty(code))
                    continue;

                var typeType = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDef)!;
                context.AddSource($"{typeType.ContainingNamespace}.{typeType.Name}Proxy.g.cs", code);
                context.ReportDiagnostic(GenerateProxyTypeProcessedInfo(typeDef));
            }
        }
        catch (Exception e) {
            context.ReportDebug(e);
            throw;
        }
    }
}
