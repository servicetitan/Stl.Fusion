Now:
- Inbound & Outbound call trackers
  - ActivitySource integration
  - Properly handle repeating inbound calls
- RpcOutboundCallPreprocessors
    - Default / send timeout for command handler RPC calls
    - Default session
- RpcInboundCallPreprocessors
    - Default session
- Robust reconnection tests
- Robust cleanup:
  - Compute method call cleanup tests
  - Ensure all incoming calls are cancelled when RpcHub/RpcPeer is disposed.
- BackendStatus 

Near-term:
- Robust routing tests
- InvalidationInfoProvider.IsClientComputeServiceCommand & command execution w/ routers
- ComputeServiceExt.IsClient
