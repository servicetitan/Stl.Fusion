namespace Stl.Fusion.EntityFramework;

public abstract record DbHint(Symbol Value);

public record DbLockingHint(Symbol Value) : DbHint(Value)
{
    public static readonly DbLockingHint KeyShare = new(nameof(KeyShare));
    public static readonly DbLockingHint Share = new(nameof(Share));
    public static readonly DbLockingHint NoKeyUpdate = new(nameof(NoKeyUpdate));
    public static readonly DbLockingHint Update = new(nameof(Update));
}

public record DbWaitHint(Symbol Value) : DbHint(Value)
{
    public static readonly DbLockingHint NoWait = new(nameof(NoWait));
    public static readonly DbLockingHint SkipLocked = new(nameof(SkipLocked));
}

public record DbCustomHint(Symbol Value) : DbHint(Value);
