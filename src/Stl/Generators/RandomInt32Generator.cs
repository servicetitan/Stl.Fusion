using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Stl.Generators
{
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
}
