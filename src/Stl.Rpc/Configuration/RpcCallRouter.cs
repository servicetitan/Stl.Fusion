using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc;

public delegate RpcPeer? RpcCallRouter(MethodDef methodDef, ArgumentList arguments);
