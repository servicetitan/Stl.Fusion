using System;
using System.Collections.Concurrent;
using Stl.Reflection;

namespace Stl.Extensibility
{
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

        public HandlerProvider(Type handlerType) : this(TypeArgumentHandlerFactory(handlerType)) { }
        public HandlerProvider(Func<Type, IHandler> handlerFactory)
        {
            HandlerFactory = handlerFactory;
            _handlers = new ConcurrentDictionary<Type, IHandler>();
        }

        public IHandler this[Type forType] 
            => _handlers.GetOrAdd(
                forType, 
                (forType1, self) => self.HandlerFactory.Invoke(forType1), 
                this);

        // Default handler factories

        public static Func<Type, IHandler> TypeArgumentHandlerFactory(Type handlerType)
        {
            if (!handlerType.IsGenericTypeDefinition)
                throw new ArgumentOutOfRangeException(nameof(handlerType));

            IHandler CreateHandler(Type forType)
                => (IHandler) handlerType.MakeGenericType(forType).CreateInstance();

            return CreateHandler;
        }

        public static Func<Type, IHandler> TypeArgumentHandlerFactory<T>(Type handlerType, T arg)
        {
            if (!handlerType.IsGenericTypeDefinition)
                throw new ArgumentOutOfRangeException(nameof(handlerType));

            IHandler CreateHandler(Type forType)
                => (IHandler) handlerType.MakeGenericType(forType).CreateInstance(arg);

            return CreateHandler;
        }
    }
}
