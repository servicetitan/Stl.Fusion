using Stl.Conversion.Internal;

namespace Stl.Conversion;

public interface IConverter<in TSource, TTarget>
{
    Option<TTarget> TryConvert(TSource source);
#if NETFRAMEWORK || NETSTANDARD2_0
    TTarget Convert(TSource source);
#else
    TTarget Convert(TSource source)
    {
        var targetOpt = TryConvert(source);
        return targetOpt.HasValue
            ? targetOpt.ValueOrDefault!
            : throw Errors.CantConvert(typeof(TSource), typeof(TTarget));
    }
#endif
}
