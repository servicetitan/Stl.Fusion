namespace Stl.Generators.Internal;
using static SyntaxFactory;

public static class SyntaxHelpers
{
    public static InvocationExpressionSyntax EmptyArrayExpression<TItem>()
        => EmptyArrayExpression(typeof(TItem).ToTypeRef());
    public static InvocationExpressionSyntax EmptyArrayExpression(TypeSyntax itemTypeRef)
        => InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    AliasQualifiedName(
                        IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                        IdentifierName("System")),
                    IdentifierName("Array")),
                GenericName(Identifier("Empty"))
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList(
                                itemTypeRef
                            )))));

    public static ImplicitArrayCreationExpressionSyntax ImplicitArrayCreationExpression(params ExpressionSyntax[] itemExpressions)
        => SyntaxFactory.ImplicitArrayCreationExpression(
            InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                CommaSeparatedList(itemExpressions)));

    public static FieldDeclarationSyntax PrivateFieldDef(TypeSyntax type, SyntaxToken name, bool isReadonly = false)
        => PrivateFieldDef(type, name, null, isReadonly);
    public static FieldDeclarationSyntax PrivateFieldDef(TypeSyntax type, SyntaxToken name, ExpressionSyntax? initializer, bool isReadonly = false)
    {
        var initializerClause = initializer == null
            ? null
            : EqualsValueClause(initializer);
        var fieldDeclaration = FieldDeclaration(
            VariableDeclaration(type)
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(name, null, initializerClause))));
        return fieldDeclaration.WithModifiers(isReadonly
            ? TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
            : TokenList(Token(SyntaxKind.PrivateKeyword)));
    }

    public static LocalDeclarationStatementSyntax VarStatement(SyntaxToken name, ExpressionSyntax initializer)
        => VariableDeclarator(name)
            .WithInitializer(EqualsValueClause(initializer))
            .ToVarStatement();

    public static ThrowStatementSyntax ThrowStatement<TException>(string message)
        where TException : Exception
        => SyntaxFactory.ThrowStatement(
            ObjectCreationExpression(typeof(TException).ToTypeRef())
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(message)))))));

    public static ThrowExpressionSyntax ThrowExpression<TException>(string message)
        where TException : Exception
        => SyntaxFactory.ThrowExpression(
            ObjectCreationExpression(typeof(TException).ToTypeRef())
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(message)))))));

    public static AssignmentExpressionSyntax CoalesceAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
        => AssignmentExpression(SyntaxKind.CoalesceAssignmentExpression, left, right);

    public static SeparatedSyntaxList<TNode> CommaSeparatedList<TNode>(params TNode[] nodes)
        where TNode : SyntaxNode
        => CommaSeparatedList((IEnumerable<TNode>)nodes);

    public static SeparatedSyntaxList<TNode> CommaSeparatedList<TNode>(IEnumerable<TNode> nodes)
        where TNode : SyntaxNode
    {
        var list = new List<SyntaxNodeOrToken>();
        foreach (var nodeOrToken in nodes) {
            if (list.Count > 0)
                list.Add(Token(SyntaxKind.CommaToken));
            list.Add(nodeOrToken);
        }
        return SeparatedList<TNode>(NodeOrTokenList(list));
    }

    public static IdentifierNameSyntax VarIdentifierDef()
        => IdentifierName(
            Identifier(
                TriviaList(),
                SyntaxKind.VarKeyword,
                "var",
                "var",
                TriviaList()));
}
