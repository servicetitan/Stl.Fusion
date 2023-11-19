using System.Collections.Concurrent;

namespace Stl.Generators;
using static DiagnosticsHelpers;
using static GenerationHelpers;

[Generator]
public class ProxyGenerator : IIncrementalGenerator
{
    private readonly ConcurrentDictionary<ITypeSymbol, bool> _processedTypes = new(SymbolEqualityComparer.Default);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        _processedTypes.Clear();
        var items = context.SyntaxProvider
            .CreateSyntaxProvider(CouldBeAugmented, MustAugment)
            .Where(i => i.TypeDef != null)
            .Collect();
        context.RegisterSourceOutput(items, Generate);
        _processedTypes.Clear();
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

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDef, cancellationToken);
        if (typeSymbol == null)
            return default;
        if (typeSymbol.IsSealed)
            return default;
        if (typeSymbol is { TypeKind: TypeKind.Class, IsAbstract: true })
            return default;

        var declaredAccessibility = typeSymbol.DeclaredAccessibility;
        if (declaredAccessibility != Accessibility.Public && declaredAccessibility != Accessibility.Internal)
            return default;

        var requiresProxy = typeSymbol.AllInterfaces.Any(t => Equals(t.ToFullName(), RequireAsyncProxyInterfaceName));
        if (!requiresProxy)
            return default;

        // It might be a partial class w/o generic constraint clauses (even though the type has ones),
        // so we might need to "wait" for the one with generic constraint clauses
        var hasConstraints = typeSymbol.TypeParameters.Any(p => p.HasConstraints());
        if (hasConstraints && !typeDef.ConstraintClauses.Any()) {
            WriteDebug?.Invoke($"[- Type] No constraints: {typeSymbol}");
            return default;
        }

        // There might be a few parts of the same class
        if (typeDef.Modifiers.Any(SyntaxKind.PartialKeyword) && !_processedTypes.TryAdd(typeSymbol, true)) {
            WriteDebug?.Invoke($"[- Type] Already processed: {typeSymbol}");
            return default;
        }

        return (semanticModel, typeDef);
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
                context.AddSource($"{typeType.ContainingNamespace}.{typeType.Name}{ProxyClassSuffix}.g.cs", code);
                context.ReportDiagnostic(GenerateProxyTypeProcessedInfo(typeDef));
            }
        }
        catch (Exception e) {
            context.ReportDebug(e);
            throw;
        }
    }
}
