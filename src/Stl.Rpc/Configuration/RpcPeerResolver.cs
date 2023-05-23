using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc;

public delegate RpcPeer? RpcPeerResolver(MethodDef methodDef, ArgumentList arguments);
