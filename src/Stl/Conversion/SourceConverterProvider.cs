using System;

namespace Stl.Conversion
{
    public interface ISourceConverterProvider
    {
        Type SourceType { get; }
        Converter To(Type targetType);
        Converter To<TTarget>();
    }

    public interface ISourceConverterProvider<TSource> : ISourceConverterProvider
    {
        new Converter<TSource> To(Type targetType);
        new Converter<TSource, TTarget> To<TTarget>();
    }

    public abstract class SourceConverterProvider<TSource> : ISourceConverterProvider<TSource>
    {
        public Type SourceType { get; }

        Converter ISourceConverterProvider.To(Type targetType) => To(targetType);
        Converter ISourceConverterProvider.To<TTarget>() => To(typeof(TTarget));

        public Converter<TSource, TTarget> To<TTarget>()
            => (Converter<TSource, TTarget>) To(typeof(TTarget));

        public abstract Converter<TSource> To(Type targetType);

        protected SourceConverterProvider()
            => SourceType = typeof(TSource);
    }
}
