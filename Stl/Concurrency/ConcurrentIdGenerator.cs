using System;
using System.Collections.Generic;
using System.Linq;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class ConcurrentIdGenerator<T>
    {
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount << 2;

        private readonly Func<T>[] _generators;

        public int ConcurrencyLevel => _generators.Length;

        public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory)
            : this(generatorFactory, DefaultConcurrencyLevel) { }
        public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory, int concurrencyLevel)
            : this(Enumerable.Range(0, concurrencyLevel).Select(generatorFactory)) { }
        public ConcurrentIdGenerator(IEnumerable<Func<T>> generators)
            : this(generators.ToArray()) { }
        public ConcurrentIdGenerator(Func<T>[] generators) 
            => _generators = generators;

        public T Next(int workerId)
        {
            var generator = _generators[(workerId & int.MaxValue) % ConcurrencyLevel];
            lock (generator)
                return generator.Invoke();
        }
    }
}
