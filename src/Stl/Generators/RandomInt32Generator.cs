using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Stl.Generators
{
#if !NETSTANDARD2_0
    public sealed class RandomInt32Generator : Generator<long>
    {
        private readonly int[] _buffer = new int[1];
        private readonly RandomNumberGenerator _rng;

        public RandomInt32Generator(RandomNumberGenerator? rng = null)
            => _rng = rng ?? RandomNumberGenerator.Create();

        public override long Next()
        {
            var bufferSpan = MemoryMarshal.Cast<int, byte>(_buffer.AsSpan());
            _rng!.GetBytes(bufferSpan);
            return _buffer![0];
        }
    }
#else
    public sealed class RandomInt32Generator : Generator<long>
    {
        private readonly byte[] _buffer = new byte[sizeof(int)];
        private readonly RandomNumberGenerator _rng;

        public RandomInt32Generator(RandomNumberGenerator? rng = null)
            => _rng = rng ?? RandomNumberGenerator.Create();

        public override long Next()
        {
            _rng!.GetBytes(_buffer);
            var bufferSpan = MemoryMarshal.Cast<byte, int>(_buffer.AsSpan());
            return bufferSpan![0];
        }
    }
#endif
}
