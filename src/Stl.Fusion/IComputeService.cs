using Stl.Rpc;

namespace Stl.Fusion;

// A tagging interface for proxy types
public interface IComputeService : ICommandService, IRpcClient
{ }
