using ITypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;

namespace Stl.Generators;

[Generator]
public class ProxyGenerator : IIncrementalGenerator
{
    private const string AbstractionsNamespaceName = "Stl.Interception";
    private const string GenerateProxyAttributeFullName = $"{AbstractionsNamespaceName}.GenerateProxyAttribute";

    private ITypeSymbol? _generateProxyAttributeType;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var items = context.SyntaxProvider
            .CreateSyntaxProvider(CouldBeAugmented, MustAugment)
            .Where(i => i.TypeDef != null)
            .Collect();
        context.RegisterSourceOutput(items, Generate!);
    }

    private bool CouldBeAugmented(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax or InterfaceDeclarationSyntax {
            Parent: FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax
        }; // Top-level type

    private (SemanticModel SemanticModel, TypeDeclarationSyntax? TypeDef)
        MustAugment(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;
        _generateProxyAttributeType ??= compilation.GetTypeByMetadataName(GenerateProxyAttributeFullName)!;

        var typeDef = (TypeDeclarationSyntax) context.Node;
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDef);
        if (typeSymbol == null || typeSymbol.IsSealed || !typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
            return default;
        if (typeDef is ClassDeclarationSyntax && typeSymbol.IsAbstract)
            return default;

        var generateCtorAttrDef = semanticModel.GetAttribute(_generateProxyAttributeType!, typeDef.AttributeLists);
        return generateCtorAttrDef == null ? default : (semanticModel, typeDef);
    }

    private void Generate(
        SourceProductionContext context,
        ImmutableArray<(SemanticModel SemanticModel, TypeDeclarationSyntax TypeDef)> items)
    {
        if (items.Length == 0)
            return;
        try {
            context.ReportDebug($"Found {items.Length} type(s) to generate proxies.");
            foreach (var (semanticModel, typeDef) in items) {
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
