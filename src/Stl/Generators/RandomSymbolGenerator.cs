using System.Security.Cryptography;
using Stl.Text;

namespace Stl.Generators
{
    public class RandomSymbolGenerator : Generator<Symbol>
    {
        public static readonly RandomSymbolGenerator Default = new RandomSymbolGenerator();

        protected RandomStringGenerator Rsg { get; }
        public string Prefix { get; }
        public string Alphabet { get; }
        public int Length { get; }

        public RandomSymbolGenerator(string prefix = "", int length = 12, string? alphabet = null, RandomNumberGenerator? rng = null)
        {
            Prefix = prefix;
            Length = length;
            Alphabet = alphabet;
            Rsg = new RandomStringGenerator(length, alphabet, rng);
        }

        public override Symbol Next() => Next(Length);
        public string Next(int length, string? alphabet = null)
            => Prefix + Rsg.Next(length, alphabet);
    }
}
