using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Stl.Generators.Internal.SyntaxHelpers;

namespace Stl.Generators;

public class ProxyTypeGenerator
{
    private const string StlInterception = "global::Stl.Interception";
    private const string ProxyClassSuffix = "Proxy";

    // Types
    private static readonly IdentifierNameSyntax ProxyInterfaceTypeName = IdentifierName($"{StlInterception}.IProxy");
    private static readonly IdentifierNameSyntax InterceptorTypeName = IdentifierName($"{StlInterception}.Interceptor");
    private static readonly IdentifierNameSyntax ProxyHelperTypeName = IdentifierName($"{StlInterception}.ProxyHelper");
    private static readonly IdentifierNameSyntax ArgumentListTypeName = IdentifierName($"{StlInterception}.ArgumentList");
    private static readonly GenericNameSyntax ArgumentListGenericTypeName = GenericName(ArgumentListTypeName.Identifier.Text);
    private static readonly TypeSyntax NullableMethodInfoType = NullableType(typeof(MethodInfo).ToTypeRef());
    // Methods
    private static readonly IdentifierNameSyntax ArgumentListNewMethodName = IdentifierName("New");
    private static readonly IdentifierNameSyntax ProxyHelperGetMethodInfoName = IdentifierName("GetMethodInfo");
    private static readonly IdentifierNameSyntax ProxyInterceptMethodName = IdentifierName("Intercept");
    private static readonly GenericNameSyntax ProxyInterceptGenericMethodName = GenericName(ProxyInterceptMethodName.Identifier.Text);
    private static readonly IdentifierNameSyntax ProxyInterfaceBindMethodName = IdentifierName("Bind");
    // Properties, fields, locals
    private static readonly IdentifierNameSyntax InterceptorPropertyName = IdentifierName("Interceptor");
    private static readonly IdentifierNameSyntax InterceptorFieldName = IdentifierName("_interceptor");
    private static readonly IdentifierNameSyntax InterceptorParameterName = IdentifierName("interceptor");
    private static readonly IdentifierNameSyntax ProxyTargetFieldName = IdentifierName("_proxyTarget");
    private static readonly IdentifierNameSyntax ProxyTargetParameterName = IdentifierName("proxyTarget");
    private static readonly IdentifierNameSyntax InterceptedVarName = IdentifierName("intercepted");
    private static readonly IdentifierNameSyntax MethodInfoVarName = IdentifierName("methodInfo");
    private static readonly IdentifierNameSyntax InvocationVarName = IdentifierName("invocation");

    public string GeneratedCode { get; } = "";

    private SourceProductionContext Context { get; }
    private SemanticModel SemanticModel { get; }
    private TypeDeclarationSyntax TypeDef { get; }
    private ITypeSymbol TypeSymbol { get; } = null!;
    private ClassDeclarationSyntax? ClassDef { get; }
    private InterfaceDeclarationSyntax? InterfaceDef { get; }
    private bool IsInterfaceProxy => InterfaceDef != null;

    private string ProxyTypeName { get; } = "";
    private ClassDeclarationSyntax ProxyDef { get; } = null!;
    private List<MemberDeclarationSyntax> ProxyFields { get; } = new();
    private List<MemberDeclarationSyntax> ProxyProperties { get; } = new();
    private List<MemberDeclarationSyntax> ProxyConstructors { get; } = new();
    private List<MemberDeclarationSyntax> ProxyMethods { get; } = new();

    public ProxyTypeGenerator(SourceProductionContext context, SemanticModel semanticModel, TypeDeclarationSyntax typeDef)
    {
        Context = context;
        SemanticModel = semanticModel;
        TypeDef = typeDef;
        if (SemanticModel.GetDeclaredSymbol(TypeDef) is not { } typeSymbol)
            return;

        Context.ReportDebug($"Generating proxy for '{typeDef.Identifier.Text}'...");
        TypeSymbol = typeSymbol;
        ClassDef = TypeDef as ClassDeclarationSyntax;
        InterfaceDef = TypeDef as InterfaceDeclarationSyntax;

        var ns = TypeDef.GetNamespaceRef();
        var typeFullNameDef = TypeDef.ToTypeRef();
        ProxyTypeName = TypeDef.Identifier.Text + ProxyClassSuffix;
        ProxyDef = ClassDeclaration(ProxyTypeName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithTypeParameterList(TypeDef.TypeParameterList)
            .WithBaseList(BaseList(CommaSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(typeFullNameDef),
                SimpleBaseType(ProxyInterfaceTypeName))
            ))
            .WithConstraintClauses(TypeDef.ConstraintClauses);

        AddProxyInterfaceImplementation();
        if (ClassDef != null)
            AddClassConstructors();
        else
            AddInterfaceConstructors();
        AddProxyMethods();

        ProxyDef = ProxyDef
            .WithMembers(List(
                ProxyFields
                .Concat(ProxyProperties)
                .Concat(ProxyConstructors)
                .Concat(ProxyMethods)));

        // Building Compilation unit

        var syntaxRoot = SemanticModel.SyntaxTree.GetRoot();
        var unit = CompilationUnit()
            .AddUsings(syntaxRoot.ChildNodes().OfType<UsingDirectiveSyntax>().ToArray())
            .AddMembers(FileScopedNamespaceDeclaration(ns!).AddMembers(ProxyDef));

        var code = unit.NormalizeWhitespace().ToFullString();
        GeneratedCode = "// Generated code" + Environment.NewLine +
            "#nullable enable" + Environment.NewLine +
            code;
    }

    private void AddInterfaceConstructors()
    {
        var proxyTargetType = NullableType(TypeDef.ToTypeRef());

        ProxyFields.Add(
            PrivateFieldDef(proxyTargetType,
                ProxyTargetFieldName.Identifier,
                LiteralExpression(SyntaxKind.NullLiteralExpression)));

        ProxyConstructors.Add(
            ConstructorDeclaration(Identifier(ProxyTypeName))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList())
                .WithBody(Block()));

        ProxyConstructors.Add(
            ConstructorDeclaration(Identifier(ProxyTypeName))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(ProxyTargetParameterName.Identifier)
                                .WithType(NullableType(TypeDef.ToTypeRef())))))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        ProxyTargetFieldName),
                                    ProxyTargetParameterName))))));
    }

    private void AddClassConstructors()
    {
        foreach (var originalCtor in TypeDef.Members.OfType<ConstructorDeclarationSyntax>()) {
            var parameters = new List<SyntaxNodeOrToken>();
            foreach (var parameter in originalCtor.ParameterList.Parameters) {
                if (parameters.Count > 0)
                    parameters.Add(Token(SyntaxKind.CommaToken));
                parameters.Add(Argument(IdentifierName(parameter.Identifier.Text)));
            }

            ProxyConstructors.Add(
                ConstructorDeclaration(Identifier(ProxyTypeName))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(originalCtor.ParameterList)
                    .WithInitializer(
                        ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList<ArgumentSyntax>(parameters))))
                    .WithBody(Block()));
        }
    }

    private void AddProxyMethods()
    {
        var methodIndex = 0;
        foreach (var method in GetProxyMethods()) {
            var modifiers = TokenList(
                method.DeclaredAccessibility.HasFlag(Accessibility.Protected)
                    ? Token(SyntaxKind.ProtectedKeyword)
                    : Token(SyntaxKind.PublicKeyword));
            if (!IsInterfaceProxy)
                modifiers = modifiers.Add(Token(SyntaxKind.OverrideKeyword));

            var returnType = method.ReturnType.ToTypeRef();
            var parameters = ParameterList(CommaSeparatedList(
                method.Parameters.Select(p =>
                    Parameter(Identifier(p.Name))
                        .WithType(p.Type.ToTypeRef()))));

            var cachedInterceptedFieldName = "_cachedIntercepted" + methodIndex;
            var cachedMethodInfoFieldName = "_cachedMethodInfo" + methodIndex;
            ProxyFields.Add(CachedInterceptedFieldDef(Identifier(cachedInterceptedFieldName), returnType));
            ProxyFields.Add(PrivateFieldDef(NullableMethodInfoType, Identifier(cachedMethodInfoFieldName)));

            var interceptedLambda = CreateInterceptedLambda(method, parameters);
            var getMethodInfo = GetMethodInfoExpression(TypeDef, method, parameters);
            var newArgumentList = method.Parameters
                .Select(p => Argument(IdentifierName(p.Name)))
                .ToArray();

            var body = Block(
                VarStatement(InterceptedVarName.Identifier,
                    CoalesceAssignmentExpression(IdentifierName(cachedInterceptedFieldName), interceptedLambda)),
                VarStatement(MethodInfoVarName.Identifier,
                    CoalesceAssignmentExpression(IdentifierName(cachedMethodInfoFieldName), getMethodInfo)),
                VarStatement(InvocationVarName.Identifier,
                    CreateInvocationInstance(
                        ThisExpression(),
                        PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, MethodInfoVarName),
                        NewArgumentList(newArgumentList),
                        InterceptedVarName)),
                InvokeProxyIntercept(returnType, InvocationVarName)
                    .ToStatement(!returnType.IsVoid())
            );

            ProxyMethods.Add(
                MethodDeclaration(returnType, Identifier(method.Name))
                    .WithModifiers(modifiers)
                    .WithParameterList(parameters)
                    .WithBody(body));
            methodIndex++;
        }
    }

    private IEnumerable<IMethodSymbol> GetProxyMethods()
    {
        foreach (var member in TypeSymbol.GetMembers()) {
            if (member is not IMethodSymbol method)
                continue;
            if (method.MethodKind is not MethodKind.Ordinary)
                continue;
            if (method.IsSealed || method.IsStatic || method.IsGenericMethod)
                continue;
            if (!(method.DeclaredAccessibility.HasFlag(Accessibility.Public)
                    || method.DeclaredAccessibility.HasFlag(Accessibility.Protected)))
                continue;

            if (!IsInterfaceProxy) {
                if (method.IsAbstract || !method.IsVirtual)
                    continue;
            }

            yield return method;
        }
    }

    private static ObjectCreationExpressionSyntax CreateInvocationInstance(params ExpressionSyntax[] ctorArguments)
        => ObjectCreationExpression(IdentifierName("Invocation"))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(ctorArguments.Select(c => Argument(c)))));

    private ExpressionSyntax GetMethodInfoExpression(
        TypeDeclarationSyntax typeDeclaration,
        IMethodSymbol method,
        ParameterListSyntax parameters)
    {
        var parameterTypes = parameters.Parameters
            .Select(p => TypeOfExpression(p.Type!))
            .ToArray<ExpressionSyntax>();

        var typeFullNameDef = typeDeclaration.ToTypeRef();
        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ProxyHelperTypeName,
                ProxyHelperGetMethodInfoName))
            .WithArgumentList(
                ArgumentList(
                    CommaSeparatedList(
                        Argument(
                            TypeOfExpression(typeFullNameDef)),
                        Argument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(method.Name))),
                        Argument(
                            parameterTypes.Length == 0
                            ? EmptyArrayExpression<Type>()
                            : ImplicitArrayCreationExpression(parameterTypes))
                    )));
    }

    private FieldDeclarationSyntax CachedInterceptedFieldDef(SyntaxToken name, TypeSyntax returnTypeDef)
    {
        TypeSyntax fieldTypeDef;
        if (!returnTypeDef.IsVoid()) {
            fieldTypeDef = GenericName(Identifier("global::System.Func"))
                .WithTypeArgumentList(
                    TypeArgumentList(CommaSeparatedList(ArgumentListTypeName, returnTypeDef)));
        }
        else {
            fieldTypeDef = GenericName(Identifier("global::System.Action"))
                .WithTypeArgumentList(
                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(ArgumentListTypeName)));
        }
        return PrivateFieldDef(NullableType(fieldTypeDef), name);
    }

    private SimpleLambdaExpressionSyntax CreateInterceptedLambda(IMethodSymbol method, ParameterListSyntax parameters)
    {
        var typedArgsVarGenericArguments = parameters.Parameters.Select(p => p.Type!).ToArray();

        var typeArgsVariableType =
                typedArgsVarGenericArguments.Length == 0
                ? (NameSyntax) ArgumentListTypeName
                : ArgumentListGenericTypeName.WithTypeArgumentList(
                    TypeArgumentList(CommaSeparatedList(typedArgsVarGenericArguments)));

        var args = IdentifierName("args");
        var typedArgs = IdentifierName("typedArgs");
        var proxyTargetCallArguments = new List<ArgumentSyntax>();
        for (var i = 0; i < parameters.Parameters.Count; i++) {
            var argumentList = IdentifierName("Item" + i);
            proxyTargetCallArguments.Add(Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typedArgs,
                    argumentList)));
        }

        var baseRef = IsInterfaceProxy
            ? PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, ProxyTargetFieldName)
            : (ExpressionSyntax) BaseExpression();
        var baseInvocation = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                baseRef,
                IdentifierName(method.Name)),
            ArgumentList(CommaSeparatedList(proxyTargetCallArguments))
        );

        var baseInvocationBlock = Block(
            VarStatement(typedArgs.Identifier, CastExpression(typeArgsVariableType, args)),
            baseInvocation.ToStatement(!method.ReturnType.ToTypeRef().IsVoid()));

        return SimpleLambdaExpression(Parameter(args.Identifier))
            .WithBlock(baseInvocationBlock);
    }

    private InvocationExpressionSyntax InvokeProxyIntercept(TypeSyntax genericArguments, params ExpressionSyntax[] arguments)
    {
        var methodName = genericArguments.IsVoid()
            ? (SimpleNameSyntax)ProxyInterceptMethodName
            : ProxyInterceptGenericMethodName
                .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(genericArguments)));
        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    InterceptorPropertyName,
                    methodName))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(arguments.Select(Argument))));
    }

    private InvocationExpressionSyntax NewArgumentList(IEnumerable<ArgumentSyntax> newArgumentListParams)
        => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ArgumentListTypeName,
                    ArgumentListNewMethodName))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(newArgumentListParams)));

    private void AddProxyInterfaceImplementation()
    {
        ProxyFields.Add(
            FieldDeclaration(
                VariableDeclaration(NullableType(InterceptorTypeName))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(InterceptorFieldName.Identifier))))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword))));

        var interceptorGetterDef = Block(
            IfStatement(
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    InterceptorFieldName,
                    LiteralExpression(
                        SyntaxKind.NullLiteralExpression)),
                ThrowStatement(
                    ObjectCreationExpression(
                            IdentifierName(nameof(InvalidOperationException)))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal("This proxy has no interceptor - you must call Bind method first."))
                                    )))))),
            ReturnStatement(InterceptorFieldName));

        ProxyProperties.Add(
            PropertyDeclaration(InterceptorTypeName, InterceptorPropertyName.Identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                .WithAccessorList(
                    AccessorList(
                        SingletonList(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithBody(interceptorGetterDef)))));

        ProxyMethods.Add(
            MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                ProxyInterfaceBindMethodName.Identifier)
            .WithExplicitInterfaceSpecifier(
                ExplicitInterfaceSpecifier(ProxyInterfaceTypeName))
            .WithParameterList(ParameterList(
                    SingletonSeparatedList(
                        Parameter(InterceptorParameterName.Identifier)
                            .WithType(InterceptorTypeName))))
            .WithBody(
                Block(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            InterceptorFieldName,
                            LiteralExpression(
                                SyntaxKind.NullLiteralExpression)),
                        ThrowStatement<InvalidOperationException>("Interceptor is already bound.")),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            InterceptorFieldName,
                            BinaryExpression(
                                SyntaxKind.CoalesceExpression,
                                InterceptorParameterName,
                                ThrowExpression<ArgumentNullException>(InterceptorParameterName.Identifier.Text)))))));
    }
}
