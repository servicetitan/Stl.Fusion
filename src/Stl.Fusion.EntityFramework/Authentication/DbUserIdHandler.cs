using System;
using Stl.Conversion;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbUserIdHandler<TDbUserId>
        where TDbUserId : notnull
    {
        TDbUserId NewId();
        string Format(TDbUserId userId);
        TDbUserId Parse(string formattedUserId);
        Option<TDbUserId> TryParse(string formattedUserId);
    }

    public class DbUserIdHandler<TDbUserId> : IDbUserIdHandler<TDbUserId>
        where TDbUserId : notnull
    {
        protected IConverter<string, TDbUserId> Parser { get; }
        protected IConverter<TDbUserId, string> Formatter { get; }
        protected Func<TDbUserId> Generator { get; }

        public DbUserIdHandler(IConverterProvider converters, Func<TDbUserId>? generator = null)
        {
            if (generator == null) {
                generator = () => default!;
                if (typeof(TDbUserId) == typeof(string)) {
                    var rsg = new RandomStringGenerator(12, RandomStringGenerator.Base32Alphabet);
                    generator = () => (TDbUserId) (object) rsg.Next();
                }
            }
            Parser = converters.From<string>().To<TDbUserId>();
            Formatter = converters.From<TDbUserId>().To<string>();
            Generator = generator;
        }

        public TDbUserId NewId()
            => Generator.Invoke();
        public string Format(TDbUserId userId)
            => Formatter.Convert(userId);
        public TDbUserId Parse(string formattedUserId)
            => Parser.Convert(formattedUserId);
        public Option<TDbUserId> TryParse(string formattedUserId)
            => Parser.TryConvert(formattedUserId);
    }
}
