using Stl.Conversion.Internal;

namespace Stl.Conversion
{
    public static class ConverterExt
    {
        public static Converter ThrowIfUnavailable(this Converter converter)
            => converter.IsAvailable
                ? converter
                : throw Errors.NoConverter(converter.SourceType, converter.TargetType);

        public static Converter<TSource> ThrowIfUnavailable<TSource>(this Converter<TSource> converter)
            => converter.IsAvailable
                ? converter
                : throw Errors.NoConverter(converter.SourceType, converter.TargetType);

        public static Converter<TSource, TTarget> ThrowIfUnavailable<TSource, TTarget>(this Converter<TSource, TTarget> converter)
            => converter.IsAvailable
                ? converter
                : throw Errors.NoConverter(converter.SourceType, converter.TargetType);
    }
}
