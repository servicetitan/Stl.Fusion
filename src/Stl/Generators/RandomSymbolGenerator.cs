using System.Security.Cryptography;

namespace Stl.Generators;

// Thread-safe!
public class RandomSymbolGenerator : Generator<Symbol>
{
    public static readonly RandomSymbolGenerator Default = new();

    protected RandomStringGenerator Rsg { get; }
    public string Prefix { get; }
    public string Alphabet { get; }
    public int Length { get; }

    public RandomSymbolGenerator(string prefix = "", int length = 12, string? alphabet = null, RandomNumberGenerator? rng = null)
    {
        Prefix = prefix;
        Rsg = new RandomStringGenerator(length, alphabet, rng);
        Alphabet = Rsg.Alphabet;
        Length = Rsg.Length;
    }

    public override Symbol Next() => Next(Length);
    public string Next(int length, string? alphabet = null)
        => Prefix + Rsg.Next(length, alphabet);
}
