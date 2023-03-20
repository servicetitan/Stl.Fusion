using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Stl.Generators.Internal.GenerationHelpers;

namespace Stl.Generators.Internal;

public static class GenerationExt
{
    public static List<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol typeSymbol, bool includingSelf = false)
        => typeSymbol.GetAllBaseTypes(includingSelf, new());
    public static List<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol typeSymbol, bool includingSelf, List<ITypeSymbol> output)
    {
        var current = typeSymbol;
        while (current != null) {
            if (includingSelf || !ReferenceEquals(current, typeSymbol))
                output.Add(current);
            current = current.BaseType;
        }
        return output;
    }

    public static List<ITypeSymbol> GetAllInterfaces(this ITypeSymbol typeSymbol, bool includingSelf = false)
        => typeSymbol.GetAllInterfaces(includingSelf, new(), new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    public static List<ITypeSymbol> GetAllInterfaces(
        this ITypeSymbol typeSymbol, bool includingSelf,
        List<ITypeSymbol> output, HashSet<ITypeSymbol> traversed)
    {
        if (includingSelf && typeSymbol.TypeKind == TypeKind.Interface) {
            if (!traversed.Add(typeSymbol))
                return output;
            output.Add(typeSymbol);
        }

        // DFS traversal
        foreach (var baseInterface in typeSymbol.Interfaces)
            baseInterface.GetAllInterfaces(true, output, traversed);
        return output;
    }

    public static (string? Name, TypeSyntax TypeRef) GetMemberNameAndTypeRef(this MemberDeclarationSyntax memberDef)
    {
        if (memberDef is FieldDeclarationSyntax f) {
            var typeDef = f.Declaration.Type;
            var vars = f.Declaration.Variables;
            if (vars.Count != 1)
                return default;
            var name = vars[0].Identifier.Text;
            return (name, typeDef);
        }
        if (memberDef is PropertyDeclarationSyntax p) {
            var typeDef = p.Type;
            var name = p.Identifier.Text;
            return (name, typeDef);
        }
        return default;
    }

    public static NameSyntax? GetNamespaceRef(this TypeDeclarationSyntax typeDef)
    {
        if (typeDef.Parent is NamespaceDeclarationSyntax ns)
            return ns.Name;
        if (typeDef.Parent is FileScopedNamespaceDeclarationSyntax fns)
            return fns.Name;
        return null;
    }

    public static TypeSyntax ToTypeRef(this TypeDeclarationSyntax typeDef)
    {
        var ns = typeDef.GetNamespaceRef()?.ToString() ?? "";
        return ParseTypeName(
            string.IsNullOrEmpty(ns)
                ? typeDef.Identifier.Text
                : $"{ns.WithGlobalPrefix()}.{typeDef.Identifier.Text}");
    }

    public static TypeSyntax ToTypeRef(this Type type)
    {
        if (!type.IsGenericType)
            return ParseTypeName(string.IsNullOrEmpty(type.Namespace)
                ? type.Name
                : $"{type.Namespace.WithGlobalPrefix()}.{type.Name}");

        // Get the C# representation of the generic type minus its type arguments.
        var name = type.Name.Replace('+', '.');
        name = name.Substring(0, name.IndexOf("`", StringComparison.Ordinal));

        var args = type.GetGenericArguments();
        return GenericName(
            Identifier(name),
            TypeArgumentList(SeparatedList(args.Select(ToTypeRef)))
        );
    }

    public static TypeSyntax ToTypeRef(this ITypeSymbol typeSymbol)
        => ParseTypeName(typeSymbol.ToGlobalName());

    public static string ToFullName(this ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
        ));
        const string systemPrefix = "System.";
        return name.StartsWith(systemPrefix, StringComparison.Ordinal)
            ? Simplify(name.Substring(systemPrefix.Length), name)
            : name;
    }

    public static string ToGlobalName(this ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Included,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
        ));
        const string globalSystemPrefix = "global::System.";
        return name.StartsWith(globalSystemPrefix, StringComparison.Ordinal)
            ? Simplify(name.Substring(globalSystemPrefix.Length), name)
            : name;
    }

    public static string WithGlobalPrefix(this string ns)
    {
        if (string.IsNullOrEmpty(ns))
            return "global::";
        if (ns.StartsWith("global::", StringComparison.Ordinal))
            return ns;
        return "global::" + ns;
    }

    public static bool IsVoid(this TypeSyntax typeSyntax)
        => typeSyntax is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);

    public static bool IsObject(this TypeSyntax typeSyntax)
        => typeSyntax is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.ObjectKeyword);

    // Helpers

    private static string Simplify(string shortName, string fullName)
        => shortName switch {
            "Boolean" => "bool",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Char" => "char",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Single" => "float",
            "Double" => "double",
            "String" => "string",
            "Object" => "object",
            "Void" => "void",
            _ => fullName,
        };
}
