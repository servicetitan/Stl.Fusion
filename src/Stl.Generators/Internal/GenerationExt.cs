using System.Globalization;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Stl.Generators.Internal.SyntaxHelpers;

namespace Stl.Generators.Internal;

public static class GenerationExt
{
    public static IEnumerable<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol typeSymbol, bool includingSelf = false)
    {
        var current = typeSymbol;
        while (current != null) {
            if (includingSelf || !ReferenceEquals(current, typeSymbol))
                yield return current;
            current = current.BaseType;
        }
    }

    public static IEnumerable<ITypeSymbol> GetAllInterfaces(this ITypeSymbol typeSymbol, bool includingSelf = false)
    {
        if (includingSelf && typeSymbol.TypeKind == TypeKind.Interface)
            yield return typeSymbol;

        // DFS traversal
        foreach (var i in typeSymbol.Interfaces)
            foreach (var iBase in i.GetAllInterfaces(true))
                yield return iBase;
    }

    public static INamedTypeSymbol? GetTypeFor<T>(this Compilation compilation)
    {
        var type = typeof(T);
        var fullName = $"{type.Namespace}.{type.Name}";
        return compilation.GetTypeByMetadataName(fullName);
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

    public static AttributeSyntax? GetAttribute(
        this SemanticModel semanticModel,
        ITypeSymbol attributeType,
        SyntaxList<AttributeListSyntax> attributeLists)
    {
        var attributes =
            from l in attributeLists
            from a in l.Attributes
            let aType = semanticModel.GetTypeInfo(a).Type
            where SymbolEqualityComparer.Default.Equals(aType, attributeType)
            select a;
        return attributes.FirstOrDefault();
    }

    public static AttributeArgumentSyntax? GetNamedArgument(this AttributeSyntax attributeDef, string argumentName)
    {
        var arguments =
            from a in attributeDef.ArgumentList?.Arguments ?? default
            where Equals(argumentName, a?.NameEquals?.Name.Identifier.Text)
            select a;
        return arguments.SingleOrDefault();
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
        => ParseTypeName(typeSymbol.ToFullName());

    public static string ToFullName(this ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Included,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
        ));
        const string globalSystem = "global::System.";
        return name.StartsWith(globalSystem, StringComparison.Ordinal)
            ? Simplify(name.Substring(globalSystem.Length), name)
            : name;
    }

    public static string ToVariableName(this string? s)
    {
        if (s == null)
            return null!;
        var sb = new StringBuilder("@");
        var mustChangeCase = true;
        foreach (var c in s) {
            if (mustChangeCase && !char.IsUpper(c)) {
                mustChangeCase = false;
                var lastIndex = sb.Length - 1;
                if (lastIndex >= 2)
                    sb[lastIndex] = char.ToUpper(sb[lastIndex], CultureInfo.InvariantCulture);
            }
            if (c == '@' && sb.Length <= 1)
                continue;
            sb.Append(mustChangeCase ? char.ToLower(c, CultureInfo.InvariantCulture) : c);
        }
        return sb.ToString();
    }

    public static bool IsVoid(this TypeSyntax typeSyntax)
        => typeSyntax is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);

    public static bool IsObject(this TypeSyntax typeSyntax)
        => typeSyntax is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.ObjectKeyword);

    public static LocalDeclarationStatementSyntax ToVarStatement(this VariableDeclaratorSyntax variable)
        => LocalDeclarationStatement(VariableDeclaration(VarIdentifierDef())
            .WithVariables(SingletonSeparatedList(variable)));

    public static StatementSyntax ToStatement(this ExpressionSyntax expression, bool isReturnStatement = false)
        => isReturnStatement
            ? ReturnStatement(expression)
            : ExpressionStatement(expression);

    public static string WithGlobalPrefix(this string ns)
    {
        if (string.IsNullOrEmpty(ns))
            return "global::";
        if (ns.StartsWith("global::", StringComparison.Ordinal))
            return ns;
        return "global::" + ns;
    }

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
