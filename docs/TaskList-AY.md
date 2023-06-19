Now:
- Robust reconnection tests
  - Compute method call tests
- Inbound & Outbound call trackers
  - ActivitySource integration
- RpcOutboundMiddleware
  - Command calls should wait for peer connection
  - Timeout for command calls
- RpcInboundMiddleware
    - Default session
- BackendStatus 

Near-term:
- Robust routing tests
- InvalidationInfoProvider.IsClientComputeServiceCommand & command execution w/ routers
- ComputeServiceExt.IsClient
