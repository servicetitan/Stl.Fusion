using System;

namespace Stl.Fusion
{
    public readonly struct ComputeContextScope : IDisposable
    {
        private readonly ComputeContext _previousContext;
        public ComputeContext Context { get; }

        internal ComputeContextScope(ComputeContext context)
        {
            Context = context;
            _previousContext = ComputeContext.Current;
            ComputeContext.Current = context;
        }

        public void Dispose()
            => ComputeContext.Current = _previousContext;
    }
}
