namespace Stl.Conversion.Internal;

public class FuncConverter<TSource, TTarget> : Converter<TSource, TTarget>
{
    public Func<TSource, TTarget> Converter { get; init; }
    public Func<TSource, Option<TTarget>> TryConverter { get; init; }

    public override TTarget Convert(TSource source)
        => Converter.Invoke(source);
    public override object? ConvertUntyped(object? source)
        => Converter((TSource) source!);

    public override Option<TTarget> TryConvert(TSource source)
        => TryConverter.Invoke(source).Cast<TTarget>();
    public override Option<object?> TryConvertUntyped(object? source)
        => source is TSource t ? TryConverter.Invoke(t).Cast<object?>() : Option<object?>.None;

    public FuncConverter(
        Func<TSource, TTarget> converter,
        Func<TSource, Option<TTarget>> tryConverter)
    {
        Converter = converter;
        TryConverter = tryConverter;
    }
}

public static class FuncConverter<TSource>
{
    public static FuncConverter<TSource, TTarget> New<TTarget>(Func<TSource, TTarget> converter)
        => new(converter, ToTryConvert(converter));
    public static FuncConverter<TSource, TTarget> New<TTarget>(
        Func<TSource, Option<TTarget>> tryConverter,
        Func<TSource, TTarget>? converter)
        => new(converter ?? FromTryConvert(tryConverter), tryConverter);

    public static Func<TSource, TTarget> FromTryConvert<TTarget>(Func<TSource, Option<TTarget>> converter)
        => s => {
            var targetOpt = converter.Invoke(s);
            return targetOpt.HasValue
                ? targetOpt.ValueOrDefault!
                : throw Errors.CantConvert(typeof(TSource), typeof(TTarget));
        };

    public static Func<TSource, Option<TTarget>> ToTryConvert<TTarget>(Func<TSource, TTarget> converter)
        => s => {
            try {
                return converter.Invoke(s);
            }
            catch {
                // Intended
                return Option<TTarget>.None;
            }
        };
}
