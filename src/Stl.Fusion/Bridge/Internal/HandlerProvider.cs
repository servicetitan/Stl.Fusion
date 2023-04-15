namespace Stl.Fusion.Bridge.Internal;

public class HandlerProvider<TArg, TResult>
{
    public interface IHandler
    {
        TResult Handle(object target, TArg arg);
    }

    public interface IHandler<T> : IHandler
    { }

    private readonly ConcurrentDictionary<Type, IHandler> _handlers;

    public Func<Type, IHandler> HandlerFactory { get; }

    public HandlerProvider(Type handlerType) : this(DefaultHandlerFactory(handlerType)) { }
    public HandlerProvider(Func<Type, IHandler> handlerFactory)
    {
        HandlerFactory = handlerFactory;
        _handlers = new ConcurrentDictionary<Type, IHandler>();
    }

    public IHandler this[Type forType]
        => _handlers.GetOrAdd(forType,
            static (forType1, self) => self.HandlerFactory(forType1),
            this);

    // Default handler factory

    private static Func<Type, IHandler> DefaultHandlerFactory(Type handlerType)
    {
        if (!handlerType.IsGenericTypeDefinition)
            throw new ArgumentOutOfRangeException(nameof(handlerType));

        IHandler CreateHandler(Type forType)
            => (IHandler) handlerType.MakeGenericType(forType).CreateInstance();

        return CreateHandler;
    }
}
