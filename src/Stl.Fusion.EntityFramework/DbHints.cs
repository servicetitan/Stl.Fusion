namespace Stl.Fusion.EntityFramework;

public abstract record DbHint(Symbol Value) { }

public record DbLockingHint(Symbol Value) : DbHint(Value)
{
    public static DbLockingHint KeyShare { get; } = new(nameof(KeyShare));
    public static DbLockingHint Share { get; } = new(nameof(Share));
    public static DbLockingHint NoKeyUpdate { get; } = new(nameof(NoKeyUpdate));
    public static DbLockingHint Update { get; } = new(nameof(Update));
}

public record DbWaitHint(Symbol Value) : DbHint(Value)
{
    public static DbLockingHint NoWait { get; } = new(nameof(NoWait));
    public static DbLockingHint SkipLocked { get; } = new(nameof(SkipLocked));
}

public record DbCustomHint(Symbol Value) : DbHint(Value) { }
