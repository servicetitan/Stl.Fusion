Now:
- RpcOutboundMiddleware
  - Command calls should wait for peer connection
  - Timeout for command calls
- RpcInboundMiddleware
  - Default session
  - ActivitySource integration

Near-term:
- Robust routing tests
- InvalidationInfoProvider.IsClientComputeServiceCommand & command execution w/ routers
- ComputeServiceExt.IsClient
