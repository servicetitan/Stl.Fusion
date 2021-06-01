using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Stl.Generators
{
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
}
