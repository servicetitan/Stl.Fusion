using System.Threading;

namespace Stl.Generators
{
    public sealed class SequentialInt32Generator : Generator<int>
    {
        public static readonly SequentialInt32Generator Default = new SequentialInt32Generator();

        private int _counter;

        public SequentialInt32Generator(int start = 1)
            => _counter = start - 1;

        public override int Next()
            => Interlocked.Increment(ref _counter);
    }
}
