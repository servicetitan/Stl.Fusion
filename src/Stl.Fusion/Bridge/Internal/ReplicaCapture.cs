using System;
using System.Threading;
using Stl.Internal;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReplicaCapture : IDisposable
    {
        internal static readonly AsyncLocal<ReplicaCapture?> CurrentLocal = new AsyncLocal<ReplicaCapture?>();
        private readonly ReplicaCapture? _oldCurrent;
        private volatile IReplica? _replica;
        
        public IReplica? Replica => _replica;

        public ReplicaCapture()
        {
            _oldCurrent = CurrentLocal.Value;
            CurrentLocal.Value = this;
        }

        public void Dispose() => CurrentLocal.Value = _oldCurrent;

        public IReplica<T> GetCapturedReplica<T>()
        {
            var replica = Replica;
            if (replica == null) {
                var replicaType = typeof(T);
                if (replicaType == typeof(string))
                    throw Fusion.Internal.Errors.UnsupportedReplicaType(replicaType);
                throw Errors.InternalError("Replica wasn't captured.");
            }
            return (IReplica<T>) replica;
        }

        public static void Capture(IReplica? value)
        {
            var replicaCapture = CurrentLocal.Value;
            if (replicaCapture == null)
                throw Errors.InternalError($"Missing {nameof(ReplicaCapture)}.");
            if (null != Interlocked.CompareExchange(ref replicaCapture._replica, value, null))
                throw Errors.InternalError($"{nameof(ReplicaCapture)} already captured replica.");
        }
    }
}
