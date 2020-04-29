using System;

namespace Stl.Purifier.Internal
{
    public readonly struct ComputeContextUseScope : IDisposable
    {
        private readonly bool _resetIsUsed;
        public ComputeContext Context { get; }

        internal ComputeContextUseScope(ComputeContext context, bool markAsUsed)
        {
            Context = context;
            _resetIsUsed = markAsUsed;
        }

        public void Dispose()
        {
            if (_resetIsUsed)
                Context.ResetIsUsed();
        }
    }
}
