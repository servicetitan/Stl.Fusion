using System;

namespace Stl.Extensibility
{
    public class Factory<T>
    {
        public readonly Func<T> Delegate;

        public Factory(Func<T> factory) => Delegate = factory;

        public override string ToString() => $"{GetType().Name}({Delegate})";

        public static implicit operator Factory<T>(Func<T> factory) => new Factory<T>(factory);

        public T Create() => Delegate.Invoke();
    }
}
