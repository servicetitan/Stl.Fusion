namespace Stl.Interception.Internal;

public abstract class InterfaceProxy
{
    public abstract object? ProxyTargetUntyped { get; set; }
}

public abstract class InterfaceProxy<TProxyTarget> : InterfaceProxy
    where TProxyTarget : class
{
    public TProxyTarget? ProxyTarget { get; set; }

    public override object? ProxyTargetUntyped {
        get => ProxyTarget;
        set => ProxyTarget = (TProxyTarget?)value;
    }
}
