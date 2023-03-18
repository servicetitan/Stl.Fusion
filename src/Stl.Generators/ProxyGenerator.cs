using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stl.Generators;

[Generator]
public class ProxyGenerator : IIncrementalGenerator
{
    private const string AbstractionsNamespaceName = "Stl.Interception";
    private const string GenerateProxyAttributeFullName = $"{AbstractionsNamespaceName}.GenerateProxyAttribute";
    private const string ProxyInterfaceTypeName = "IProxy";
    private const string ProxyInterfaceBindMethodName = "Bind";
    private const string ProxyClassSuffix = "Proxy";
    private const string InterceptorTypeName = "Interceptor";
    private const string InterceptorPropertyName = "Interceptor";
    private const string InterceptMethodName = "Intercept";
    private const string ArgumentListTypeName = "ArgumentList";
    private const string ArgumentListNewMethodName = "New";
    private const string SubjectFieldName = "_subject";
    private const string InterceptedLocalVarName = "intercepted";
    private const string MethodInfoLocalVarName = "methodInfo";
    private const string InvocationLocalVarName = "invocation";

    private ITypeSymbol? _generateProxyAttributeType;
    private readonly QualifiedNameSyntax _interceptionNs;
    private readonly TypeSyntax _methodInfoTypeSyntax;

    public ProxyGenerator()
    {
        _interceptionNs = QualifiedName(IdentifierName("Stl"), IdentifierName("Interception"));
        _methodInfoTypeSyntax = NullableType(typeof(MethodInfo).ToTypeName());
    }

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
        var generateCtorAttrDef = semanticModel.GetAttribute(_generateProxyAttributeType!, typeDef.AttributeLists);
        return generateCtorAttrDef == null ? default : (semanticModel, typeDef);
    }

    private void Generate(
        SourceProductionContext context,
        ImmutableArray<(SemanticModel SemanticModel, TypeDeclarationSyntax TypeDef)> items)
    {
        if (items.Length == 0)
            return;
#if DEBUG
        try {
            context.ReportDiagnostic(DebugWarning($"Found {items.Length} type(s) to generate proxy."));
            GenerateImpl(context, items);
        }
        catch (Exception e) {
            // This code allows to see the stack trace
            context.ReportDiagnostic(DebugWarning(e));
        }
#else
        GenerateImpl(context, items);
#endif
    }

    private void GenerateImpl(
        SourceProductionContext context,
        ImmutableArray<(SemanticModel SemanticModel, TypeDeclarationSyntax TypeDef)> items)
    {
        foreach (var item in items) {
            var code = GenerateCode(context, item.SemanticModel, item.TypeDef);
            var typeType = (ITypeSymbol)item.SemanticModel.GetDeclaredSymbol(item.TypeDef)!;
            context.AddSource($"{typeType.ContainingNamespace}.{typeType.Name}Proxy.g.cs", code);
            context.ReportDiagnostic(GenerateProxyTypeProcessedInfo(item.TypeDef));
        }
    }

    private TypeSyntax GetTypeFullNameSyntax(TypeDeclarationSyntax typeDef)
    {
        var ns = typeDef.GetNamespaceName();
        var name = IdentifierName(typeDef.Identifier.Text);
        return ns != null ? QualifiedName(ns, name) : name;
    }

    private string GenerateCode(SourceProductionContext context, SemanticModel semanticModel, TypeDeclarationSyntax typeDef)
    {
#if DEBUG        
        context.ReportDiagnostic(DebugWarning($"About to generate proxy for '{typeDef.Identifier.Text}'."));
#endif        
        var originalClassDef = typeDef as ClassDeclarationSyntax;
        var ns = typeDef.GetNamespaceName();

        var originalTypeFullNameSyntax = GetTypeFullNameSyntax(typeDef);
        var classDef = ClassDeclaration(typeDef.Identifier.Text + ProxyClassSuffix)
            .WithBaseList(BaseList(CommaSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(originalTypeFullNameSyntax),
                SimpleBaseType(QualifiedName(_interceptionNs, IdentifierName(ProxyInterfaceTypeName))))
            ));
        if (originalClassDef != null) {
            classDef = classDef
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithTypeParameterList(originalClassDef.TypeParameterList);
        }
        else {
            classDef = classDef
                .AddModifiers(Token(SyntaxKind.PublicKeyword));
        }

        var classMembers = new List<MemberDeclarationSyntax>();

        AddMethodOverrides(classMembers, context, typeDef);

#if DEBUG        
        context.ReportDiagnostic(DebugWarning($"{classMembers.Count} class members added for method overrides."));
#endif        

        AddInterceptor(classMembers);

        if (originalClassDef != null) {
            AddClassConstructors(classMembers, originalClassDef, classDef.Identifier.Text);
        }
        else {
            classMembers.Add(PrivateFieldDeclaration(SubjectFieldName, originalTypeFullNameSyntax, true));
            AddInterfaceProxyConstructor(classMembers, originalTypeFullNameSyntax, classDef.Identifier.Text);
        }

        classDef = classDef.WithMembers(List(classMembers));

        // Building Compilation unit

        var syntaxRoot = semanticModel.SyntaxTree.GetRoot();
        var unit = CompilationUnit()
            .AddUsings(syntaxRoot.ChildNodes().OfType<UsingDirectiveSyntax>().ToArray())
            .AddMembers(FileScopedNamespaceDeclaration(ns!).AddMembers(classDef));

        var code = unit.NormalizeWhitespace().ToFullString();
        return "// Generated code" + Environment.NewLine +
            "#nullable enable" + Environment.NewLine +
            code;
    }

    private void AddInterfaceProxyConstructor(ICollection<MemberDeclarationSyntax> classMembers, TypeSyntax interfaceFullNameSyntax, string className)
    {
        const string subjectCtorParameterName = "subject";
        var ctorDef = ConstructorDeclaration(Identifier(className))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(Identifier(subjectCtorParameterName))
                            .WithType(interfaceFullNameSyntax))))
            .WithBody(
                Block(
                    SingletonList<StatementSyntax>(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName(SubjectFieldName)),
                                IdentifierName(subjectCtorParameterName))))));
        classMembers.Add(ctorDef);
    }

    private static void AddClassConstructors(ICollection<MemberDeclarationSyntax> classMembers,
        ClassDeclarationSyntax originalClassDef, string className)
    {
        foreach (var originalCtor in originalClassDef.Members.OfType<ConstructorDeclarationSyntax>()) {
            var parameters = new List<SyntaxNodeOrToken>();
            foreach (var parameter in originalCtor.ParameterList.Parameters) {
                if (parameters.Count > 0)
                    parameters.Add(Token(SyntaxKind.CommaToken));
                parameters.Add(Argument(IdentifierName(parameter.Identifier.Text)));
            }

            var ctorDef = ConstructorDeclaration(Identifier(className))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(originalCtor.ParameterList)
                .WithInitializer(
                    ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SeparatedList<ArgumentSyntax>(parameters))))
                .WithBody(Block());
            classMembers.Add(ctorDef);
        }
    }

    private void AddMethodOverrides(
        ICollection<MemberDeclarationSyntax> classMembers,
        SourceProductionContext context,
        TypeDeclarationSyntax originalTypeDef)
    {
        var methodIndex = 0;
        var isInterfaceProxy = originalTypeDef is InterfaceDeclarationSyntax;

        foreach (var method in originalTypeDef.Members.OfType<MethodDeclarationSyntax>()) {
            var modifiers = method.Modifiers;
            SyntaxToken[] methodModifiers;
            if (!isInterfaceProxy) {
                var isPublic = modifiers.Any(c => c.IsKind(SyntaxKind.PublicKeyword));
                var isProtected = modifiers.Any(c => c.IsKind(SyntaxKind.ProtectedKeyword));
                var isPrivate = !isPublic && !isProtected;
                if (isPrivate)
                    continue;
                var isVirtual = modifiers.Any(c => c.IsKind(SyntaxKind.VirtualKeyword));
                if (!isVirtual) {
                    context.ReportDiagnostic(DebugWarning($"method is not virtual and not private: {method.ToString()}."));
                    continue;
                }
                var accessModifier = isPublic ? Token(SyntaxKind.PublicKeyword)
                    : isProtected ? Token(SyntaxKind.ProtectedKeyword)
                    : throw new InvalidOperationException("Wrong access modifer");
                methodModifiers = new [] {
                    accessModifier, Token(SyntaxKind.SealedKeyword), Token(SyntaxKind.OverrideKeyword)
                };
            }
            else {
                methodModifiers = new [] { Token(SyntaxKind.PublicKeyword) };
            }

            var cachedInterceptedFieldName = "_cachedIntercepted" + methodIndex;
            var cachedMethodInfoFieldName = "_cachedMethodInfo" + methodIndex;

            classMembers.Add(CachedInterceptedFieldDeclaration(cachedInterceptedFieldName, method.ReturnType));
            classMembers.Add(PrivateFieldDeclaration(cachedMethodInfoFieldName, _methodInfoTypeSyntax));

            var interceptedLambda = CreateInterceptedLambda(method, isInterfaceProxy);

            var getMethodInfoExpression = GetMethodInfoExpression(originalTypeDef, method);

            var newArgumentListParams = method.ParameterList
                .Parameters
                .Select(p => Argument(IdentifierName(p.Identifier)))
                .ToArray();

            var methodBody = Block(
                DeclareLocalVar(InterceptedLocalVarName,
                    CoalesceAssignmentExpression(IdentifierName(cachedInterceptedFieldName), interceptedLambda)),
                DeclareLocalVar(MethodInfoLocalVarName,
                    CoalesceAssignmentExpression(IdentifierName(cachedMethodInfoFieldName), getMethodInfoExpression)),
                DeclareLocalVar(InvocationLocalVarName,
                    CreateInvocationInstance(
                        ThisExpression(),
                        PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, IdentifierName(MethodInfoLocalVarName)),
                        IdentifierName(InterceptedLocalVarName),
                        NewArgumentList(newArgumentListParams))),
                CallIntercept(method.ReturnType, IdentifierName(InvocationLocalVarName))
                    .ToLastBlockStatement(!method.ReturnType.IsVoid())
            );

            var interceptedMethod = MethodDeclaration(method.ReturnType, method.Identifier)
                .WithModifiers(TokenList(methodModifiers))
                .WithParameterList(method.ParameterList)
                .WithBody(methodBody);

            classMembers.Add(interceptedMethod);

            methodIndex++;
        }
    }

    private static ObjectCreationExpressionSyntax CreateInvocationInstance(params ExpressionSyntax[] ctorArguments)
        => ObjectCreationExpression(IdentifierName("Invocation"))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(ctorArguments.Select(c => Argument(c)))));

    private ExpressionSyntax GetMethodInfoExpression(
        TypeDeclarationSyntax typeDeclaration,
        MethodDeclarationSyntax method)
    {
        var methodParameterTypes = method.ParameterList.Parameters
            .Select(p => TypeOfExpression(p.Type!))
            .ToArray<ExpressionSyntax>();
        var typeFullName = GetTypeFullNameSyntax(typeDeclaration);
        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("GenerateProxyHelper"),
                    IdentifierName("GetMethodInfo")))
            .WithArgumentList(
                ArgumentList(
                    CommaSeparatedList(
                        Argument(
                            TypeOfExpression(typeFullName)),
                        Argument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(method.Identifier.Text))),
                        Argument(
                            ImplicitArrayCreationExpression(
                                InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    CommaSeparatedList(methodParameterTypes
                                    ))))
                    )));
    }

    private FieldDeclarationSyntax CachedInterceptedFieldDeclaration(string fieldName, TypeSyntax returnType)
    {
        TypeSyntax fieldType;
        if (!returnType.IsVoid()) {
            fieldType = GenericName(Identifier("global::System.Func"))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        CommaSeparatedList(
                            QualifiedName(_interceptionNs, IdentifierName(ArgumentListTypeName)),
                            returnType
                        )));
        }
        else {
            fieldType = GenericName(Identifier("global::System.Action"))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(
                            QualifiedName(_interceptionNs, IdentifierName(ArgumentListTypeName))
                        )));
        }
        return PrivateFieldDeclaration(fieldName, NullableType(fieldType));
    }

    private static FieldDeclarationSyntax PrivateFieldDeclaration(string fieldName, TypeSyntax fieldType, bool isReadonly = false)
    {
        var fieldDeclaration = FieldDeclaration(
            VariableDeclaration(fieldType)
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(fieldName))))
        );
        if (isReadonly)
            return fieldDeclaration
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));
        return fieldDeclaration
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
    }

    private SimpleLambdaExpressionSyntax CreateInterceptedLambda(MethodDeclarationSyntax method, bool isInterfaceProxy)
    {
        var typedArgsVarGenericArguments = method.ParameterList
            .Parameters.Select(p => p.Type!).ToArray();

        var typeArgsVariableType = QualifiedName(_interceptionNs,
            GenericName(Identifier(ArgumentListTypeName))
                .WithTypeArgumentList(
                    TypeArgumentList(CommaSeparatedList(typedArgsVarGenericArguments))));

        const string lambdaParameterName = "args";
        var typedArgsVariable = VariableDeclarator(Identifier("typedArgs"))
            .WithInitializer(EqualsValueClause(
                CastExpression(typeArgsVariableType, IdentifierName(lambdaParameterName)
                )));

        var subjectCallArguments = new List<ArgumentSyntax>();
        for (int itemId = 0; itemId < method.ParameterList.Parameters.Count; itemId++) {
            var argumentListPropertyName = "Item" + itemId;
            subjectCallArguments.Add(Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(typedArgsVariable.Identifier.Text),
                    IdentifierName(argumentListPropertyName))));
        }

        var methodSubjectCall = !isInterfaceProxy
            ? (ExpressionSyntax) BaseExpression()
            : IdentifierName(SubjectFieldName);

        var interceptedBlock = Block(
            LocalDeclarationStatement(
                VariableDeclaration(VarIdentifier())
                    .WithVariables(SingletonSeparatedList(typedArgsVariable))),
            InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        methodSubjectCall,
                        IdentifierName(method.Identifier.Text)),
                    ArgumentList(CommaSeparatedList(subjectCallArguments))
                )
                .ToLastBlockStatement(!method.ReturnType.IsVoid())
        );

        var lambdaExpression = SimpleLambdaExpression(Parameter(Identifier(lambdaParameterName)))
            .WithBlock(interceptedBlock);
        return lambdaExpression;
    }

    private InvocationExpressionSyntax CallIntercept(TypeSyntax genericArguments, params ExpressionSyntax[] arguments)
    {
        var methodName = genericArguments.IsVoid()
            ? (SimpleNameSyntax)IdentifierName(InterceptMethodName)
            : GenericName(Identifier(InterceptMethodName))
                .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(genericArguments)));
        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(InterceptorPropertyName),
                    methodName))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(arguments.Select(Argument))));
    }

    private InvocationExpressionSyntax NewArgumentList(IEnumerable<ArgumentSyntax> newArgumentListParams)
        => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    QualifiedName(_interceptionNs, IdentifierName(ArgumentListTypeName)),
                    IdentifierName(ArgumentListNewMethodName)))
            .WithArgumentList(
                ArgumentList(CommaSeparatedList(newArgumentListParams)));

    private void AddInterceptor(ICollection<MemberDeclarationSyntax> classMembers)
    {
        const string interceptorFieldName = "_interceptor";
        var interceptorType = QualifiedName(_interceptionNs, IdentifierName(InterceptorTypeName));
        var interceptorField = FieldDeclaration(
                VariableDeclaration(NullableType(interceptorType))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(interceptorFieldName)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
        classMembers.Add(interceptorField);

        var interceptorProperty =
            PropertyDeclaration(interceptorType, Identifier(InterceptorPropertyName))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                .WithAccessorList(
                    AccessorList(
                        SingletonList(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithBody(
                                    Block(
                                        IfStatement(
                                            BinaryExpression(
                                                SyntaxKind.EqualsExpression,
                                                IdentifierName(interceptorFieldName),
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
                                                                        Literal(
                                                                            "Bind Proxy with Interceptor first.")))))))),
                                        ReturnStatement(
                                            IdentifierName(interceptorFieldName)))))));
        classMembers.Add(interceptorProperty);

        const string interceptorParameterName = "interceptor";
        var bindInterceptorMethod = MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier(ProxyInterfaceBindMethodName))
            .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(QualifiedName(_interceptionNs, IdentifierName(ProxyInterfaceTypeName))))
            .WithParameterList(ParameterList(
                    SingletonSeparatedList(
                        Parameter(Identifier(interceptorParameterName))
                            .WithType(interceptorType))))
            .WithBody(
                Block(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            IdentifierName(interceptorFieldName),
                            LiteralExpression(
                                SyntaxKind.NullLiteralExpression)),
                        ThrowStatement(
                            ObjectCreationExpression(
                                    QualifiedName(IdentifierName("System"), IdentifierName("InvalidOperationException")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal("Interceptor is bound already.")))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(interceptorFieldName),
                            BinaryExpression(
                                SyntaxKind.CoalesceExpression,
                                IdentifierName(interceptorParameterName),
                                ThrowExpression(
                                    ObjectCreationExpression(
                                            IdentifierName("ArgumentNullException"))
                                        .WithArgumentList(
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            Literal(interceptorParameterName))))))))))));
        classMembers.Add(bindInterceptorMethod);
    }

    private static SeparatedSyntaxList<TNode> CommaSeparatedList<TNode>(params TNode[] nodes) where TNode : SyntaxNode
        => CommaSeparatedList((IEnumerable<TNode>)nodes);

    private static SeparatedSyntaxList<TNode> CommaSeparatedList<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode
    {
        var list = new List<SyntaxNodeOrToken>();
        foreach (var nodeOrToken in nodes) {
            if (list.Count > 0)
                list.Add(Token(SyntaxKind.CommaToken));
            list.Add(nodeOrToken);
        }
        return SeparatedList<TNode>(NodeOrTokenList(list));
    }

    private static LocalDeclarationStatementSyntax DeclareLocalVar(string localVarName, ExpressionSyntax initExpression)
        => LocalDeclarationStatement(
            VariableDeclaration(VarIdentifier())
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(localVarName))
                            .WithInitializer(
                                EqualsValueClause(initExpression)))));

    private static AssignmentExpressionSyntax CoalesceAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
        => AssignmentExpression(SyntaxKind.CoalesceAssignmentExpression, left, right);

    private static IdentifierNameSyntax VarIdentifier()
        => IdentifierName(
            Identifier(
                TriviaList(),
                SyntaxKind.VarKeyword,
                "var",
                "var",
                TriviaList()));
}
