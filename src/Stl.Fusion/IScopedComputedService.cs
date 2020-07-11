namespace Stl.Fusion
{
    // A tagging interface for IComputedService or IReplicaService
    // that must be registered as scoped in the container.
    public interface IScopedComputedService : IComputedService { }
}
