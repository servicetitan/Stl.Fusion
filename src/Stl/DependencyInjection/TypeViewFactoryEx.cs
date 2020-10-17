using System;

namespace Stl.DependencyInjection
{
    public static class TypeViewFactoryEx
    {
        public readonly struct Builder<TView>
        {
            public ITypeViewFactory Factory { get; }

            public Builder(ITypeViewFactory factory) => Factory = factory;

            public TView For(Type implementationType, object implementation)
                => (TView) Factory.Create(implementation, implementationType, typeof(TView));

            public TView For<TImplementation>(TImplementation implementation)
                where TImplementation : class
                => (TView) Factory.Create(implementation, typeof(TImplementation), typeof(TView));
        }

        public static Builder<TView> Create<TView>(this ITypeViewFactory factory)
            where TView : class
            => new Builder<TView>(factory);
    }
}
