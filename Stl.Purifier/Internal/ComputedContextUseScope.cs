using System;

namespace Stl.Purifier.Internal
{
    public readonly struct ComputedContextUseScope : IDisposable
    {
        private readonly bool _resetIsUsed;
        public ComputeContext Context { get; }

        internal ComputedContextUseScope(ComputeContext context, bool markAsUsed)
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
