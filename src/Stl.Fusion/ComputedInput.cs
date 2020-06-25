using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public abstract class ComputedInput : IEquatable<ComputedInput>
    {
        public IFunction Function { get; protected set; } = null!;
        public int HashCode { get; protected set; }

        protected ComputedInput() { }
        protected ComputedInput(IFunction function)
        {
            Function = function;
            HashCode = function.GetHashCode();
        }

        public override string ToString() => $"{Function}(...)";

        // Conversion to IComputed

        public IComputed? TryGetCachedComputed(IComputed? usedBy = null) 
            => Function.TryGetCached(this, usedBy);

        public IComputed? TryGetCachedComputed(LTag lTag, IComputed? usedBy = null)
        {
            var computed = TryGetCachedComputed(usedBy);
            return computed == null ? computed : computed.LTag == lTag ? computed : null;
        }

        public Task<IComputed> GetComputedAsync(IComputed? usedBy = null, 
            ComputeContext? context = null, 
            CancellationToken cancellationToken = default) 
            => Function.InvokeAsync(this, usedBy, context, cancellationToken);

        // Equality

        public abstract bool Equals(ComputedInput other);
        public override bool Equals(object? obj) 
            => obj is ComputedInput other && Equals(other);
        
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode;

        public static bool operator ==(ComputedInput? left, ComputedInput? right) 
            => Equals(left, right);
        public static bool operator !=(ComputedInput? left, ComputedInput? right) 
            => !Equals(left, right);
    }
}
