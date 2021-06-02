using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace Stl.Mathematics
{
    public static class MathEx
    {
        private static readonly ConcurrentDictionary<int, BigInteger> _factorials =
            new ConcurrentDictionary<int, BigInteger>();

        public static long Min(long a, long b) => a <= b ? a : b;
        public static long Max(long a, long b) => a >= b ? a : b;

        public static ulong Min(ulong a, ulong b) => a <= b ? a : b;
        public static ulong Max(ulong a, ulong b) => a >= b ? a : b;

        public static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a%b);
        public static BigInteger Gcd(BigInteger a, BigInteger b) => b == 0 ? a : Gcd(b, a%b);

        public static long Lcm(long a, long b) => a*b / Gcd(a,b);
        public static BigInteger Lcm(BigInteger a, BigInteger b) => a*b / Gcd(a,b);

        public static long ExtendedGcd(long a, long b, ref long x, ref long y)
        {
            if (b==0) {
                x = 1;
                y = 0;
                return a;
            } else {
                var g = ExtendedGcd(b, a%b, ref y, ref x);
                y -= a/b*x;
                return g;
            }
        }

        public static BigInteger Factorial(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            if (n <= 1)
                return BigInteger.One;
            var i = n;
            var f = BigInteger.One;
            while (i >= 1 && !_factorials.TryGetValue(n, out f)) i--;
            if (i <= 1)
                f = BigInteger.One;
            for (i++; i <= n; i++) {
                f *= i;
                _factorials.TryAdd(i, f);
            }
            return f;
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

        public static unsafe string Format(long number, string digits)
        {
            var radix = digits.Length;
            var size = radix < 10 ? 65 : 21; // Just to simplify the calc.
            Span<char> buffer = stackalloc char[size];
#if !NETSTANDARD2_0
            return new String(FormatTo(number, digits, buffer));
#else
            return FormatTo(number, digits, buffer).ToString();
#endif
        }

        public static Span<char> FormatTo(long number, string digits, Span<char> buffer)
        {
            var radix = digits.Length;
            if (radix < 2)
                throw new ArgumentOutOfRangeException(nameof(digits));

            var sDigits = digits.AsSpan();
            if (number == 0) {
                buffer[0] = sDigits[0];
                return buffer.Slice(0, 1);
            }
            var index = buffer.Length;
            var n = Math.Abs(number);
            while (n != 0)  {
                var digit = (int) (n % radix);
                buffer[--index] = sDigits[digit];
                n /= radix;
            }
            if (number < 0)
                buffer[--index] = '-';
            var tail = buffer.Slice(index);
            tail.CopyTo(buffer);
            return buffer.Slice(0, tail.Length);
        }

        public static long Parse(string number, string digits)
            => Parse(number.AsSpan(), digits);
        public static long Parse(ReadOnlySpan<char> number, string digits)
            => TryParse(number, digits, out var result)
                ? result
                : throw new ArgumentOutOfRangeException(nameof(number));

        public static bool TryParse(string number, string digits, out long result)
            => TryParse(number.AsSpan(), digits, out result);
        public static bool TryParse(ReadOnlySpan<char> number, string digits, out long result)
        {
            var radix = digits.Length;
            if (radix < 2)
                throw new ArgumentOutOfRangeException(nameof(digits));

            result = 0;
            if (number.IsEmpty)
                return false;

            var sDigits = digits.AsSpan();
            var sign = 1L;
            if (number[0] == '-') {
                sign = -1;
                number = number.Slice(1);
            }
            var multiplier = 1L;
            for (var i = number.Length - 1; i >= 0; i--) {
                var c = number[i];
                var digit = sDigits.IndexOf(c);
                if (digit == -1)
                    return false;

                result += digit * multiplier;
                multiplier *= radix;
            }
            result *= sign;
            return true;
        }
    }
}
