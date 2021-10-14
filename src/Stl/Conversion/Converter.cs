using System;
using Stl.Conversion.Internal;

namespace Stl.Conversion
{
    public abstract class Converter
    {
        public Type SourceType { get; init; } = null!;
        public Type TargetType { get; init; } = null!;
        public abstract bool IsAvailable { get; }

        public abstract object? ConvertUntyped(object? source);
        public abstract Option<object?> TryConvertUntyped(object? source);
    }

    public abstract class Converter<TSource> : Converter
    {
        protected Converter() => SourceType = typeof(TSource);
    }

    public abstract class Converter<TSource, TTarget> : Converter<TSource>, IConverter<TSource, TTarget>
    {
        public static Converter<TSource, TTarget> Unavailable { get; set; } =
            FuncConverter<TSource>.New<TTarget>(
                _ => throw Errors.NoConverter(typeof(TSource), typeof(TTarget)),
                _ => throw Errors.NoConverter(typeof(TSource), typeof(TTarget)));

        public override bool IsAvailable => this != Unavailable;

        public abstract TTarget Convert(TSource source);
        public abstract Option<TTarget> TryConvert(TSource source);

        protected Converter()
            => TargetType = typeof(TTarget);
    }
}
