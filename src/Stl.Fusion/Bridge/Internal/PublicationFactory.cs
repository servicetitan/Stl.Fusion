using System;
using System.Collections.Concurrent;
using Stl.Concurrency;
using Stl.Fusion.Internal;
using Stl.Reflection;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Bridge.Internal
{
    public interface IPublicationFactory
    {
        public IPublication Create(
            Type publicationType, IPublisher publisher,
            IComputed computed, Symbol publicationId, IMomentClock clock);
    }

    public sealed class PublicationFactory : IPublicationFactory
    {
        private delegate IPublication Constructor(IPublisher publisher,
            IComputed computed, Symbol publicationId, IMomentClock clock);

        private static readonly ConcurrentDictionary<Type, Constructor> ConstructorCache =
            new ConcurrentDictionary<Type, Constructor>();
        private static readonly Func<Type, Constructor> CreateCache = Create;

        public static PublicationFactory Instance { get; } = new PublicationFactory();

        private PublicationFactory() { }

        public IPublication Create(Type publicationType, IPublisher publisher,
            IComputed computed, Symbol publicationId, IMomentClock clock)
            => ConstructorCache
                .GetOrAddChecked(publicationType, CreateCache)
                .Invoke(publisher, computed, publicationId, clock);

        private static Constructor Create(Type publicationType)
        {
            if (!publicationType.IsGenericTypeDefinition)
                throw Errors.PublicationTypeMustBeOpenGenericType(nameof(publicationType));

            var handler = new FactoryApplyHandler(publicationType);

            IPublication Factory(IPublisher publisher, IComputed computed, Symbol publicationId, IMomentClock clock)
                => computed.Apply(handler, (publisher, publicationId, clock));

            return Factory;
        }

        private class FactoryApplyHandler : IComputedApplyHandler<
            (IPublisher Publisher, Symbol PublicationId, IMomentClock Clock),
            IPublication>
        {
            private readonly Type _publicationType;
            private readonly ConcurrentDictionary<Type, Type> _closedTypeCache =
                new ConcurrentDictionary<Type, Type>();

            public FactoryApplyHandler(Type publicationType)
                => _publicationType = publicationType;

            public IPublication Apply<TIn, TOut>(
                IComputed<TIn, TOut> computed,
                (IPublisher Publisher, Symbol PublicationId, IMomentClock Clock) arg)
                where TIn : ComputedInput
            {
                var closedType = _closedTypeCache.GetOrAddChecked(
                    typeof(TOut),
                    (tArg, tGeneric) => tGeneric.MakeGenericType(tArg),
                    _publicationType);
                return (IPublication) closedType.CreateInstance(
                    _publicationType, arg.Publisher,
                    (IComputed<TOut>) computed, arg.PublicationId, arg.Clock);
            }
        }
    }
}
