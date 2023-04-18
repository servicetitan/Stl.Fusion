using System.ComponentModel;

namespace Stl.Conversion.Internal;

public class DefaultSourceConverterProvider<TSource> : SourceConverterProvider<TSource>
{
    private readonly ConcurrentDictionary<Type, Converter> _cache = new();

    protected IServiceProvider Services { get; }

    public DefaultSourceConverterProvider(IServiceProvider services)
        => Services = services;

    public override Converter<TSource> To(Type targetType)
        => (Converter<TSource>) _cache.GetOrAdd(targetType, static (targetType1, self) => {
            var mGetConverter = self.GetType()
                .GetMethod(nameof(GetConverter), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(targetType1);
            return (Converter) mGetConverter.Invoke(self, Array.Empty<object>())!;
        }, this);

    protected virtual Converter<TSource, TTarget> GetConverter<TTarget>()
    {
        var tSource = typeof(TSource);
        var tTarget = typeof(TTarget);

        // 0. Can we simply cast to base / descendant?
        if (tTarget.IsAssignableFrom(tSource)) {
            var pInstance =
                typeof(CastToBaseConverter<,>)
                    .MakeGenericType(tSource, tTarget)
                    .GetProperty(
                        nameof(CastToBaseConverter<TTarget, TTarget>.Instance),
                        BindingFlags.Static | BindingFlags.Public);
            return (Converter<TSource, TTarget>) pInstance!.GetValue(null)!;
        }
        if (tSource.IsAssignableFrom(tTarget)) {
            var pInstance =
                typeof(CastToDescendantConverter<,>)
                    .MakeGenericType(tSource, tTarget)
                    .GetProperty(
                        nameof(CastToDescendantConverter<TTarget, TTarget>.Instance),
                        BindingFlags.Static | BindingFlags.Public);
            return (Converter<TSource, TTarget>) pInstance!.GetValue(null)!;
        }

        // 1. Is there IConverter<,> service?
        var tConverter = typeof(IConverter<,>).MakeGenericType(tSource, tTarget);
        if (Services.GetService(tConverter) is IConverter<TSource, TTarget> converter)
            return new InterfaceConverter<TSource, TTarget>(converter);

        // 2. Is there Func<,> service?
        var tryConverterFn = Services.GetService<Func<TSource, Option<TTarget>>>();
        var converterFn = Services.GetService<Func<TSource, TTarget>>();
        if (tryConverterFn != null)
            return FuncConverter<TSource>.New(tryConverterFn, converterFn);
        if (converterFn != null)
            return FuncConverter<TSource>.New(converterFn);

        // 3. Does TSource impl. IConvertibleTo<Option<TTarget>> or IConvertibleTo<TTarget>?
        if (typeof(IConvertibleTo<Option<TTarget>>).IsAssignableFrom(tSource))
            return FuncConverter<TSource>.New(
                s => s is IConvertibleTo<Option<TTarget>> c ? c.Convert() : Option.None<TTarget>(), null);
        if (typeof(IConvertibleTo<TTarget>).IsAssignableFrom(tSource))
            return FuncConverter<TSource>.New(
                s => s is IConvertibleTo<TTarget> c ? c.Convert() : default!);

        // 4. Can we use .ToString()?
        if (tTarget == typeof(string))
            return (Converter<TSource, TTarget>) (object) FuncConverter<TSource>.New(
                s => s is null ? null : s.ToString());

        // 5. Can we use static TryParse(string, out TTarget) or Parse(string)?
        if (tSource == typeof(string)) {
            // TryParse
            var mTryParse = tTarget.GetMethod(
                nameof(bool.TryParse),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new [] {typeof(string), typeof(TTarget).MakeByRefType()},
                null);
            if (mTryParse != null && mTryParse.ReturnType == typeof(bool)) {
                var tryParseFn = (TryParseFunc<TTarget>)mTryParse.CreateDelegate(typeof(TryParseFunc<TTarget>));

                Option<TTarget> TryConvert(TSource source) =>
                    tryParseFn.Invoke(source, out var target)
                        ? Option.Some(target)
                        : Option.None<TTarget>();

                return FuncConverter<TSource>.New(TryConvert, null);
            }

            // Parse
            var mParse = tTarget.GetMethod(
                nameof(bool.Parse),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new [] {typeof(string)},
                null);
            if (mParse != null && mParse.ReturnType == tTarget) {
                var fn = (Func<TSource, TTarget>)mParse.CreateDelegate(typeof(Func<TSource, TTarget>));
                return FuncConverter<TSource>.New(fn);
            }
        }

        // 6. Is there a TypeConverter (slow)?
        var tc = TypeDescriptor.GetConverter(tSource);
        if (tc.CanConvertTo(tTarget))
            return FuncConverter<TSource>.New(s => (TTarget) tc.ConvertTo(s!, tTarget)!);

        return Converter<TSource, TTarget>.Unavailable;
    }

    // Nested types

    private delegate bool TryParseFunc<TTarget>(TSource source, out TTarget target);
}
