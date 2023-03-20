using Stl.Interception.Interceptors;

namespace Stl.Interception;

public interface ITypeViewFactory
{
    object CreateView(object implementation, Type implementationType, Type viewType);
    TypeViewFactory<TView> For<TView>()
        where TView : class;
}

public class TypeViewFactory : ITypeViewFactory
{
    public static ITypeViewFactory Default { get; } =
        new TypeViewFactory(new TypeViewInterceptor(DependencyInjection.ServiceProviderExt.Empty));

    protected Interceptor Interceptor { get; }

    public TypeViewFactory(TypeViewInterceptor interceptor)
        => Interceptor = interceptor;

    public object CreateView(object implementation, Type implementationType, Type viewType)
    {
        if (!(implementationType.IsClass || implementationType.IsInterface))
            throw new ArgumentOutOfRangeException(nameof(implementationType));
        if (!viewType.IsInterface)
            throw new ArgumentOutOfRangeException(nameof(viewType));

        var view = (IProxy)viewType.GetProxyType().CreateInstance(implementation);
        return Interceptor.AttachTo(view);
    }

    public TypeViewFactory<TView> For<TView>()
        where TView : class
        => new(this);
}

public readonly struct TypeViewFactory<TView>
    where TView : class
{
    public ITypeViewFactory Factory { get; }

    public TypeViewFactory(ITypeViewFactory factory) => Factory = factory;

    public TView CreateView(Type implementationType, object implementation)
        => (TView) Factory.CreateView(implementation, implementationType, typeof(TView));

    public TView CreateView<TImplementation>(TImplementation implementation)
        where TImplementation : class
        => (TView) Factory.CreateView(implementation, typeof(TImplementation), typeof(TView));
}
