namespace Stl.Conversion.Internal
{
    public class InterfaceConverter<TSource, TTarget> : Converter<TSource, TTarget>
    {
        public IConverter<TSource, TTarget> Converter { get; init; }

        public override TTarget Convert(TSource source)
            => Converter.Convert(source);
        public override object? ConvertToUntyped(TSource source)
            => Converter.Convert(source);
        public override object? ConvertFromUntyped(object? source)
            => Converter.Convert((TSource) source!);

        public override Option<TTarget> TryConvert(TSource source)
            => Converter.TryConvert(source);
        public override Option<object?> TryConvertToUntyped(TSource source)
            => Converter.TryConvert(source);
        public override Option<object?> TryConvertFromUntyped(object? source)
            => source is TSource t ? Converter.TryConvert(t) : Option<object?>.None;

        public InterfaceConverter(IConverter<TSource, TTarget> converter)
            => Converter = converter;
    }

    public static class InterfaceConverter
    {
        public static InterfaceConverter<TSource, TTarget> New<TSource, TTarget>(IConverter<TSource, TTarget> converter)
            => new(converter);
    }
}
