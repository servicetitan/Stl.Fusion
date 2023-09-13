using System.Numerics;

namespace Stl.Mathematics;

public static class MathExt
{
    private static readonly Dictionary<int, BigInteger> Factorials = new();

    public static double Clamp(this double value, double min, double max) => Math.Min(Math.Max(value, min), max);
    public static float Clamp(this float value, float min, float max) => Math.Min(Math.Max(value, min), max);
    public static int Clamp(this int value, int min, int max) => Math.Min(Math.Max(value, min), max);
    public static uint Clamp(this uint value, uint min, uint max) => Math.Min(Math.Max(value, min), max);

    public static long Min(long a, long b) => a <= b ? a : b;
    public static long Max(long a, long b) => a >= b ? a : b;
    public static long Clamp(this long value, long min, long max) => Min(Max(value, min), max);

    public static ulong Min(ulong a, ulong b) => a <= b ? a : b;
    public static ulong Max(ulong a, ulong b) => a >= b ? a : b;
    public static ulong Clamp(this ulong value, ulong min, ulong max) => Min(Max(value, min), max);

    public static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a%b);
    public static BigInteger Gcd(BigInteger a, BigInteger b) => b == 0 ? a : Gcd(b, a%b);

    public static long Lcm(long a, long b) => a*b / Gcd(a,b);
    public static BigInteger Lcm(BigInteger a, BigInteger b) => a*b / Gcd(a,b);

    public static long ExtendedGcd(long a, long b, ref long x, ref long y)
    {
        if (b == 0) {
            x = 1;
            y = 0;
            return a;
        }

        var g = ExtendedGcd(b, a%b, ref y, ref x);
        y -= a/b*x;
        return g;
    }

    public static BigInteger Factorial(int n)
    {
        if (n is < 0 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(n));
        if (n <= 1)
            return BigInteger.One;

        var i = n;
        var f = BigInteger.One;
        lock (Factorials) {
            while (i >= 1 && !Factorials.TryGetValue(n, out f)) i--;
            if (i <= 1)
                f = BigInteger.One;
            for (i++; i <= n; i++) {
                f *= i;
                Factorials.TryAdd(i, f);
            }
            return f;
        }
    }

    public static T FastPower<T>(T n, long power, T one, Func<T, T, T> multiply)
    {
        if (power < 0)
            throw new ArgumentOutOfRangeException(nameof(power));
        var r = one;
        for (; power != 0; power >>= 1, n = multiply(n, n))
            if ((power & 1) != 0)
                r = multiply(r, n);
        return r;
    }

    // Format & parse for arbitrary radix

    public static readonly string Digits64 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";

    public static string Format(long number, int radix)
        => radix <= 64
            ? Format(number, Digits64.AsSpan(0, radix))
            : throw new ArgumentOutOfRangeException(nameof(radix));

    public static string Format(ulong number, int radix)
        => radix <= 64
            ? Format(number, Digits64.AsSpan(0, radix))
            : throw new ArgumentOutOfRangeException(nameof(radix));

    public static unsafe string Format(long number, ReadOnlySpan<char> digits)
    {
        var radix = digits.Length;
        var size = radix < 10 ? 65 : 21; // Just to simplify the calculation
        Span<char> buffer = stackalloc char[size];
#if !NETSTANDARD2_0
        return new string(FormatTo(number, digits, buffer));
#else
        return FormatTo(number, digits, buffer).ToString();
#endif
    }

    public static unsafe string Format(ulong number, ReadOnlySpan<char> digits)
    {
        var radix = digits.Length;
        var size = radix < 10 ? 65 : 21; // Just to simplify the calculation
        Span<char> buffer = stackalloc char[size];
#if !NETSTANDARD2_0
        return new string(FormatTo(number, digits, buffer));
#else
        return FormatTo(number, digits, buffer).ToString();
#endif
    }

    public static Span<char> FormatTo(long number, int radix, Span<char> buffer)
        => radix <= 64
            ? FormatTo(number, Digits64.AsSpan(0, radix), buffer)
            : throw new ArgumentOutOfRangeException(nameof(radix));

    public static Span<char> FormatTo(ulong number, int radix, Span<char> buffer)
        => radix <= 64
            ? FormatTo(number, Digits64.AsSpan(0, radix), buffer)
            : throw new ArgumentOutOfRangeException(nameof(radix));

    public static Span<char> FormatTo(long number, ReadOnlySpan<char> digits, Span<char> buffer)
    {
        var radix = digits.Length;
        if (radix < 2)
            throw new ArgumentOutOfRangeException(nameof(digits));

        if (number == 0) {
            buffer[0] = digits[0];
            return buffer[..1];
        }
        var index = buffer.Length;
        var n = Math.Abs(number);
        while (n != 0)  {
            var digit = (int)(n % radix);
            buffer[--index] = digits[digit];
            n /= radix;
        }
        if (number < 0)
            buffer[--index] = '-';
        var tail = buffer[index..];
        tail.CopyTo(buffer);
        return buffer[..tail.Length];
    }

    public static Span<char> FormatTo(ulong number, ReadOnlySpan<char> digits, Span<char> buffer)
    {
        var radix = (ulong)digits.Length;
        if (radix < 2)
            throw new ArgumentOutOfRangeException(nameof(digits));

        if (number == 0) {
            buffer[0] = digits[0];
            return buffer[..1];
        }
        var index = buffer.Length;
        while (number != 0)  {
            var digit = (int)(number % radix);
            buffer[--index] = digits[digit];
            number /= radix;
        }
        var tail = buffer[index..];
        tail.CopyTo(buffer);
        return buffer[..tail.Length];
    }

    public static long ParseInt64(ReadOnlySpan<char> number, int radix)
        => radix <= 64
            ? ParseInt64(number, Digits64.AsSpan(0, radix))
            : throw new ArgumentOutOfRangeException(nameof(radix));
    public static long ParseInt64(ReadOnlySpan<char> number, ReadOnlySpan<char> digits)
        => TryParseInt64(number, digits, out var result)
            ? result
            : throw new ArgumentOutOfRangeException(nameof(number));

    public static bool TryParseInt64(ReadOnlySpan<char> number, int radix, out long result)
        => radix <= 64
            ? TryParseInt64(number, Digits64.AsSpan(0, radix), out result)
            : throw new ArgumentOutOfRangeException(nameof(radix));
    public static bool TryParseInt64(ReadOnlySpan<char> number, ReadOnlySpan<char> digits, out long result)
    {
        var radix = digits.Length;
        if (radix < 2)
            throw new ArgumentOutOfRangeException(nameof(digits));

        result = 0;
        if (number.IsEmpty)
            return false;

        var sign = 1L;
        if (number[0] == '-') {
            sign = -1;
            number = number[1..];
        }
        var multiplier = 1L;
        for (var i = number.Length - 1; i >= 0; i--) {
            var c = number[i];
            var digit = digits.IndexOf(c);
            if (digit == -1)
                return false;

            result += digit * multiplier;
            multiplier *= radix;
        }
        result *= sign;
        return true;
    }
}
