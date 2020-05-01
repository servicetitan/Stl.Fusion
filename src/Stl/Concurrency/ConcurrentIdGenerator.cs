using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Mathematics;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class ConcurrentIdGenerator<T>
    {
        private readonly Func<T>[] _generators;

        public int ConcurrencyLevel => _generators.Length;
        public int ConcurrencyLevelMask { get; }

 public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory)
            : this(generatorFactory, ConcurrentIdGenerator.DefaultConcurrencyLevel) { }
        public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory, int concurrencyLevel)
            : this(Enumerable.Range(0, concurrencyLevel).Select(generatorFactory)) { }
        public ConcurrentIdGenerator(IEnumerable<Func<T>> generators)
            : this(generators.ToArray()) { }
        public ConcurrentIdGenerator(Func<T>[] generators)
        {
            if (!Bits.IsPowerOf2((uint) generators.Length))
                throw new ArgumentOutOfRangeException(nameof(generators));
            _generators = generators;
            ConcurrencyLevelMask = generators.Length - 1;
        }

        public T Next(int workerId)
        {
            var generator = _generators[workerId & ConcurrencyLevelMask];
            lock (generator)
                return generator.Invoke();
        }
    }

    public static class ConcurrentIdGenerator
    {
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCountPo2 << 1;
        public static readonly ConcurrentIdGenerator<int> DefaultInt32 = NewInt32();
        public static readonly ConcurrentIdGenerator<long> DefaultInt64 = NewInt64();

        public static ConcurrentIdGenerator<int> NewInt32(int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = DefaultConcurrencyLevel;
            concurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            return new ConcurrentIdGenerator<int>(i => {
                var count = 0;
                return () => {
                    count += concurrencyLevel;
                    return count | i;
                };
            });
        }

        public static ConcurrentIdGenerator<long> NewInt64(int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = DefaultConcurrencyLevel;
            concurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            return new ConcurrentIdGenerator<long>(i => {
                var count = 0L;
                return () => {                  
                    count += concurrencyLevel;
                    return count | (long) i;
                };
            });
        }
    }
}
