using System;

namespace Stl.Purifier.Internal
{
    public static class Errors
    {
        public static Exception WrongComputationState(
            ComputationState expectedState, ComputationState state)
            => new InvalidOperationException(
                $"Wrong Computation.State: expected {expectedState}, was {state}.");
    }
}
