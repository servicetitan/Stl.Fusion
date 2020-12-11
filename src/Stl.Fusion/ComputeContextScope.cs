using System;

namespace Stl.Fusion
{
    public readonly struct ComputeContextScope : IDisposable
    {
        private readonly ComputeContext _oldContext;
        public ComputeContext Context { get; }

        internal ComputeContextScope(ComputeContext context)
        {
            _oldContext = ComputeContext.Current;
            Context = context;
            if (_oldContext != context)
                ComputeContext.Current = context;
        }

        public void Dispose()
        {
            if (_oldContext != Context)
                ComputeContext.Current = _oldContext;
        }
    }
}
