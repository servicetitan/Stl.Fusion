using System.Diagnostics.CodeAnalysis;
using Stl.Conversion;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Authentication;

public interface IDbUserIdHandler<TDbUserId>
{
    TDbUserId New();
    TDbUserId None { get; }

#if NETSTANDARD2_0
    bool IsNone(TDbUserId? userId);
    void Require(TDbUserId? userId);
#else
    bool IsNone([NotNullWhen(false)] TDbUserId? userId);
    void Require([NotNull] TDbUserId? userId);
#endif

    Symbol Format(TDbUserId? userId);
    TDbUserId Parse(Symbol userId, bool allowNone);
    bool TryParse(Symbol userId, bool allowNone, out TDbUserId result);
}

public class DbUserIdHandler<TDbUserId> : IDbUserIdHandler<TDbUserId>
{
    protected IConverter<string, TDbUserId?> Parser { get; init; }
    protected IConverter<TDbUserId?, string> Formatter { get; init; }
    protected Func<TDbUserId> Generator { get; init; }

    public TDbUserId None { get; init; }

    public DbUserIdHandler(IConverterProvider converters, Func<TDbUserId>? generator = null)
    {
        None = default!;
        if (typeof(TDbUserId) == typeof(string))
            None = (TDbUserId) (object) "";
        if (generator == null) {
            generator = () => default!;
            if (typeof(TDbUserId) == typeof(string)) {
                var rsg = new RandomStringGenerator(12, RandomStringGenerator.Base32Alphabet);
                generator = () => (TDbUserId) (object) rsg.Next();
            }
        }
        Parser = converters.From<string>().To<TDbUserId?>();
        Formatter = converters.From<TDbUserId?>().To<string>();
        Generator = generator;
    }

    public virtual TDbUserId New()
        => Generator();

#if NETSTANDARD2_0
    public virtual bool IsNone(TDbUserId? userId)
#else
    public virtual bool IsNone([NotNullWhen(false)] TDbUserId? userId)
#endif
        => EqualityComparer<TDbUserId>.Default.Equals(userId!, None)
            || EqualityComparer<TDbUserId>.Default.Equals(userId!, default!);

#if NETSTANDARD2_0
    public void Require(TDbUserId? userId)
#else
    public void Require([NotNull] TDbUserId? userId)
#endif
    {
        if (IsNone(userId))
            throw Errors.UserIdRequired();
    }

    public virtual Symbol Format(TDbUserId? userId)
        => IsNone(userId)
            ? Symbol.Empty
            : Formatter.Convert(userId);

    public virtual TDbUserId Parse(Symbol userId, bool allowNone)
    {
        if (!TryParse(userId, true, out var result))
            throw Errors.InvalidUserId();
        if (!allowNone && IsNone(result))
            throw Errors.UserIdRequired();
        return result;
    }

    public virtual bool TryParse(Symbol userId, bool allowNone, out TDbUserId result)
    {
        result = None;
        if (userId.IsEmpty)
            return allowNone;

        if (!Parser.TryConvert(userId).IsSome(out var parsed))
            return false;

        if (parsed is null)
            return allowNone;

        result = parsed;
        return true;
    }
}
