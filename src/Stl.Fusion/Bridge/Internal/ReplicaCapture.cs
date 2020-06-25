using System;
using System.Threading;
using Stl.Internal;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReplicaCapture : IDisposable
    {
        internal static readonly AsyncLocal<ReplicaCapture?> CurrentLocal = new AsyncLocal<ReplicaCapture?>();
        private readonly ReplicaCapture? _oldCurrent;
        private volatile IReplica? _capturedReplica;
        
        public IReplica? CapturedReplica => _capturedReplica;

        public ReplicaCapture()
        {
            _oldCurrent = CurrentLocal.Value;
            CurrentLocal.Value = this;
        }

        public void Dispose() => CurrentLocal.Value = _oldCurrent;

        public IReplica<T> GetCapturedReplica<T>()
        {
            var replica = CapturedReplica;
            if (replica == null)
                throw Errors.InternalError("Replica wasn't captured.");
            return (IReplica<T>) replica;
        }

        public static void Capture(IReplica? value)
        {
            var replicaCapture = CurrentLocal.Value;
            if (replicaCapture == null)
                throw Errors.InternalError($"Missing {nameof(ReplicaCapture)}.");
            if (null != Interlocked.CompareExchange(ref replicaCapture._capturedReplica, value, null))
                throw Errors.InternalError($"{nameof(ReplicaCapture)} already captured replica.");
        }
    }
}
