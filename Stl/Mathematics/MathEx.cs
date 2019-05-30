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
    }
}
