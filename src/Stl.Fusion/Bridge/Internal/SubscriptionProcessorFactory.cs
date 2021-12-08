using Stl.Concurrency;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Internal;

public interface ISubscriptionProcessorFactory
{
    public SubscriptionProcessor Create(Type genericType,
        IPublication publication, Channel<BridgeMessage> outgoingMessages,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services);
}

public sealed class SubscriptionProcessorFactory : ISubscriptionProcessorFactory
{
    private delegate SubscriptionProcessor Constructor(
        IPublication publication, Channel<BridgeMessage> outgoingMessages,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services);

    private static readonly ConcurrentDictionary<Type, Constructor> ConstructorCache = new();
    private static readonly Func<Type, Constructor> CreateCache = Create;

    public static SubscriptionProcessorFactory Instance { get; } = new();

    private SubscriptionProcessorFactory() { }

    public SubscriptionProcessor Create(Type genericType,
        IPublication publication, Channel<BridgeMessage> outgoingMessages,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services)
        => ConstructorCache
            .GetOrAddChecked(genericType, CreateCache)
            .Invoke(publication, outgoingMessages, subscriptionExpirationTime, clocks, services);

    private static Constructor Create(Type genericType)
    {
        if (!genericType.IsGenericTypeDefinition)
            throw Errors.TypeMustBeOpenGenericType(genericType);

        var handler = new FactoryApplyHandler(genericType);

        SubscriptionProcessor Factory(
            IPublication publication, Channel<BridgeMessage> outgoingMessages,
            TimeSpan subscribeTimeout, MomentClockSet clocks, IServiceProvider services)
            => publication.Apply(handler, (outgoingMessages, subscribeTimeout, clocks, services));

        return Factory;
    }

    private class FactoryApplyHandler : IPublicationApplyHandler<
        (Channel<BridgeMessage> OutgoingMessages,
        TimeSpan SubscriptionExpirationTime,
        MomentClockSet Clocks,
        IServiceProvider Services),
        SubscriptionProcessor>
    {
        private readonly Type _genericType;
        private readonly ConcurrentDictionary<Type, Type> _closedTypeCache = new();

        public FactoryApplyHandler(Type genericType)
            => _genericType = genericType;

        public SubscriptionProcessor Apply<T>(
            IPublication<T> publication,
            (Channel<BridgeMessage> OutgoingMessages,
                TimeSpan SubscriptionExpirationTime,
                MomentClockSet Clocks,
                IServiceProvider Services) arg)
        {
            var closedType = _closedTypeCache.GetOrAddChecked(
                typeof(T),
                (tArg, tGeneric) => tGeneric.MakeGenericType(tArg),
                _genericType);
            return (SubscriptionProcessor) closedType.CreateInstance(
                publication,
                arg.OutgoingMessages,
                arg.SubscriptionExpirationTime,
                arg.Clocks,
                arg.Services);
        }
    }
}
