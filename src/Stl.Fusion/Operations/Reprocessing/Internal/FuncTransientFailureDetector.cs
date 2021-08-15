using System;

namespace Stl.Fusion.Operations.Reprocessing.Internal
{
    internal class FuncTransientFailureDetector : TransientFailureDetector
    {
        public Func<Exception, bool> Detector { get; }

        public FuncTransientFailureDetector(Func<Exception, bool> detector)
            => Detector = detector;

        public override bool IsTransient(Exception error)
            => Detector.Invoke(error);
    }
}
