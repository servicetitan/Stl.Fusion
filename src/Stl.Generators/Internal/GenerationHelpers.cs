using System.Reflection;

namespace Stl.Generators.Internal;
using static SyntaxFactory;

public static class GenerationHelpers
{
    public const string StlInterceptionNs = "Stl.Interception";
    public const string StlInterceptionGns = $"global::{StlInterceptionNs}";
    public const string RequiresFullProxyInterfaceName = $"{StlInterceptionNs}.IRequiresFullProxy";
    public const string RequireAsyncProxyInterfaceName = $"{StlInterceptionNs}.IRequiresAsyncProxy";
    public const string ProxyIgnoreAttributeName = $"{StlInterceptionNs}.ProxyIgnoreAttribute";
    public const string ProxyClassSuffix = "Proxy";
    public const string ProxyNamespaceSuffix = "StlInterceptionProxies";

    // Types
    public static readonly IdentifierNameSyntax ProxyInterfaceTypeName = IdentifierName($"{StlInterceptionGns}.IProxy");
    public static readonly IdentifierNameSyntax InterceptorTypeName = IdentifierName($"{StlInterceptionGns}.Interceptor");
    public static readonly IdentifierNameSyntax ProxyHelperTypeName = IdentifierName($"{StlInterceptionGns}.ProxyHelper");
    public static readonly IdentifierNameSyntax ArgumentListTypeName = IdentifierName($"{StlInterceptionGns}.ArgumentList");
    public static readonly GenericNameSyntax ArgumentListGenericTypeName = GenericName(ArgumentListTypeName.Identifier.Text);
    public static readonly IdentifierNameSyntax InvocationTypeName = IdentifierName($"{StlInterceptionGns}.Invocation");
    public static readonly TypeSyntax NullableMethodInfoType = NullableType(typeof(MethodInfo).ToTypeRef());
    // Methods
    public static readonly IdentifierNameSyntax ArgumentListNewMethodName = IdentifierName("New");
    public static readonly IdentifierNameSyntax ProxyHelperGetMethodInfoName = IdentifierName("GetMethodInfo");
    public static readonly IdentifierNameSyntax ProxyInterceptMethodName = IdentifierName("Intercept");
    public static readonly GenericNameSyntax ProxyInterceptGenericMethodName = GenericName(ProxyInterceptMethodName.Identifier.Text);
    public static readonly IdentifierNameSyntax ProxyInterfaceBindMethodName = IdentifierName("Bind");
    // Properties, fields, locals
    public static readonly IdentifierNameSyntax InterceptorPropertyName = IdentifierName("Interceptor");
    public static readonly IdentifierNameSyntax InterceptorFieldName = IdentifierName("__interceptor");
    public static readonly IdentifierNameSyntax InterceptorParameterName = IdentifierName("interceptor");
    public static readonly IdentifierNameSyntax ProxyTargetFieldName = IdentifierName("__proxyTarget");
    public static readonly IdentifierNameSyntax ProxyTargetParameterName = IdentifierName("proxyTarget");
    public static readonly IdentifierNameSyntax InterceptedVarName = IdentifierName("intercepted");
    public static readonly IdentifierNameSyntax InvocationVarName = IdentifierName("invocation");

    // Helpers

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
        => LocalDeclarationStatement(
            VariableDeclaration(VarIdentifierDef())
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(name)
                        .WithInitializer(EqualsValueClause(initializer)))));

    public static StatementSyntax MaybeReturnStatement(bool mustReturn, ExpressionSyntax expression)
        => mustReturn
            ? ReturnStatement(expression)
            : ExpressionStatement(expression);

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

    public static PostfixUnaryExpressionSyntax SuppressNullWarning(ExpressionSyntax expression)
        => PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expression);

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
