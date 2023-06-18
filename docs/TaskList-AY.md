Now:
- Robust reconnection tests
- Inbound & Outbound call trackers
  - ActivitySource integration
- RpcOutboundCallPreprocessors
    - Default / send timeout for command handler RPC calls
    - Default session
- RpcInboundCallPreprocessors
    - Default session
- Robust cleanup:
  - Compute method call cleanup tests
  - Ensure all incoming calls are cancelled when RpcHub/RpcPeer is disposed.
- BackendStatus 

Near-term:
- Robust routing tests
- InvalidationInfoProvider.IsClientComputeServiceCommand & command execution w/ routers
- ComputeServiceExt.IsClient
