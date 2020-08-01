using System;

namespace Stl.DependencyInjection
{
    public interface IFactory<out T>
    {
        T Create();
    }

    public class Factory<T> : IFactory<T>
    {
        public readonly Func<T> Delegate;

        public Factory(Func<T> factory) => Delegate = factory;

        public override string ToString() => $"{GetType().Name}({Delegate})";

        public static implicit operator Factory<T>(Func<T> factory) => new Factory<T>(factory);

        public T Create() => Delegate.Invoke();
    }
}
