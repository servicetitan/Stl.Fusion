using System.Threading;

namespace Stl.Generators
{
    public sealed class SequentialInt64Generator : Generator<long>
    {
        public static readonly SequentialInt64Generator Default = new SequentialInt64Generator();

        private long _counter;

        public SequentialInt64Generator(long start = 1)
            => _counter = start - 1;

        public override long Next()
            => Interlocked.Increment(ref _counter);
    }
}
