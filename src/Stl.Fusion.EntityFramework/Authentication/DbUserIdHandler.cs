using Stl.Conversion;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Authentication;

public interface IDbUserIdHandler<TDbUserId>
{
    TDbUserId New();
    TDbUserId None { get; }

    bool IsNone(TDbUserId userId);
    string Format(TDbUserId userId);
    TDbUserId Parse(string formattedUserId);
    Option<TDbUserId> TryParse(string formattedUserId);
}

public class DbUserIdHandler<TDbUserId> : IDbUserIdHandler<TDbUserId>
{
    protected IConverter<string, TDbUserId> Parser { get; init; }
    protected IConverter<TDbUserId, string> Formatter { get; init; }
    protected Func<TDbUserId> Generator { get; init; }

    public TDbUserId None { get; init; }

    public DbUserIdHandler(IConverterProvider converters, Func<TDbUserId>? generator = null)
    {
        None = default!;
        if (generator == null) {
            generator = () => default!;
            if (typeof(TDbUserId) == typeof(string)) {
                None = (TDbUserId) (object) string.Empty;
                var rsg = new RandomStringGenerator(12, RandomStringGenerator.Base32Alphabet);
                generator = () => (TDbUserId) (object) rsg.Next();
            }
        }
        Parser = converters.From<string>().To<TDbUserId>();
        Formatter = converters.From<TDbUserId>().To<string>();
        Generator = generator;
    }

    public virtual TDbUserId New()
        => Generator();

    public virtual bool IsNone(TDbUserId userId)
        => EqualityComparer<TDbUserId>.Default.Equals(userId, None)
            || EqualityComparer<TDbUserId>.Default.Equals(userId, default!);

    public virtual string Format(TDbUserId userId)
        => IsNone(userId)
            ? string.Empty
            : Formatter.Convert(userId);

    public virtual TDbUserId Parse(string formattedUserId)
        => formattedUserId.IsNullOrEmpty()
            ? None
            : Parser.Convert(formattedUserId);

    public virtual Option<TDbUserId> TryParse(string formattedUserId)
        => formattedUserId.IsNullOrEmpty()
            ? None
            : Parser.TryConvert(formattedUserId);
}
