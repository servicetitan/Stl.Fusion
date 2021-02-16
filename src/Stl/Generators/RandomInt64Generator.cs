using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Stl.Generators
{
#if !NETSTANDARD2_0
    public sealed class RandomInt64Generator : Generator<long>
    {
        private readonly long[] _buffer = new long[1];
        private readonly RandomNumberGenerator _rng;

        public RandomInt64Generator(RandomNumberGenerator? rng = null)
            => _rng = rng ?? RandomNumberGenerator.Create();

        public override long Next()
        {
            var bufferSpan = MemoryMarshal.Cast<long, byte>(_buffer.AsSpan());
            _rng!.GetBytes(bufferSpan);
            return _buffer![0];
        }
    }
#else
    public sealed class RandomInt64Generator : Generator<long>
    {
        private readonly byte[] _buffer = new byte[sizeof(long)];
        private readonly RandomNumberGenerator _rng;

        public RandomInt64Generator(RandomNumberGenerator? rng = null)
            => _rng = rng ?? RandomNumberGenerator.Create();

        public override long Next()
        {
            _rng!.GetBytes(_buffer);
            var bufferSpan = MemoryMarshal.Cast<byte, long>(_buffer.AsSpan());
            return bufferSpan![0];
        }
    }
#endif
}
