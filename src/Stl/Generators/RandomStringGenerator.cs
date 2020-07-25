using System;
using System.Security.Cryptography;
using Stl.Collections;
using Stl.Mathematics;

namespace Stl.Generators
{
    public class RandomStringGenerator : Generator<string>, IDisposable
    {
        public static readonly string DefaultAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";
        public static readonly string Base64Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";
        public static readonly string Base32Alphabet = "0123456789abcdefghijklmnopqrstuv";
        public static readonly string Base16Alphabet = "0123456789abcdef";
        public static readonly RandomStringGenerator Default = new RandomStringGenerator();

        protected readonly RandomNumberGenerator Rng;
        protected object Lock => Rng;

        public string Alphabet { get; }
        public int Length { get; }

        public RandomStringGenerator(int length = 12, string? alphabet = null, RandomNumberGenerator? rng = null)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));
            alphabet ??= DefaultAlphabet;
            if (alphabet.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(alphabet));
            rng ??= RandomNumberGenerator.Create();

            Length = length;
            Alphabet = alphabet;
            Rng = rng;
        }

        public void Dispose() => Rng.Dispose();

        public override string Next() => Next(Length);
        public string Next(int length, string? alphabet = null)
        {
            if (alphabet == null)
                alphabet = Alphabet;
            else if (alphabet.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(alphabet));
            var buffer = MemoryBuffer<byte>.LeaseAndSetCount(length);
            try {
                lock (Lock) {
                    Rng.GetBytes(buffer.Span);
                }
                return string.Create(length, (buffer.BufferMemory, alphabet), (charSpan, arg) => {
                    var (bufferMemory, alphabet1) = arg;
                    var bufferSpan = bufferMemory.Span;
                    var alphabetSpan = alphabet1.AsSpan();
                    var alphabetLength = alphabetSpan.Length;
                    if (Bits.IsPowerOf2((uint) alphabetLength)) {
                        var alphabetMask = alphabetLength - 1;
                        for (var i = 0; i < charSpan.Length; i++)
                            charSpan[i] = alphabetSpan[bufferSpan[i] & alphabetMask];
                    }
                    else {
                        for (var i = 0; i < charSpan.Length; i++)
                            charSpan[i] = alphabetSpan[bufferSpan[i] % alphabetLength];
                    }
                });
            }
            finally {
                buffer.Release();
            }
        }
    }
}
