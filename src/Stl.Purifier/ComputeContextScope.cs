using System;

namespace Stl.Purifier
{
    public readonly struct ComputeContextScope : IDisposable
    {
        private readonly ComputeContext? _previousContext;
        public ComputeContext Context { get; }

        internal ComputeContextScope(ComputeContext context)
        {
            var current = ComputeContext.CurrentLocal;
            _previousContext = current.Value;
            current.Value = context;
            Context = context;
        }

        public void Dispose() 
            => ComputeContext.CurrentLocal.Value = _previousContext;
    }
}
