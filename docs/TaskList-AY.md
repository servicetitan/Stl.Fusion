Now:
- Rpc: use activity source 
- Default timeout for command handler RPC calls
- Robust reconnection tests 
- Robust compute method call cleanup tests
- Ensure all incoming calls are cancelled when RpcHub/RpcPeer is disposed 

Near-term:
- RpcClientPeer.Options
- Client-side computed cache
- Client-side ArgumentList preprocessing
- Robust routing tests
- InvalidationInfoProvider.IsClientComputeServiceCommand & command execution w/ routers
- ComputeServiceExt.IsClient
