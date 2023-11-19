using Stl.Fusion.Internal;

namespace Stl.Fusion;

public abstract class ComputedInput : IEquatable<ComputedInput>, IHasIsDisposed
{
    public IFunction Function { get; private set; } = null!;
#pragma warning disable CA1721
    public int HashCode { get; private set; }
#pragma warning restore CA1721
    public virtual string Category {
        get => Function.ToString() ?? "";
        init => throw Errors.ComputedInputCategoryCannotBeSet();
    }
    public virtual bool IsDisposed => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Initialize(IFunction function, int hashCode)
    {
        Function = function;
        HashCode = hashCode;
    }

    public override string ToString()
        => $"{Category} #{HashCode}";

    public abstract IComputed? GetExistingComputed();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw Stl.Internal.Errors.AlreadyDisposed(GetDisposedType());
    }

    // Equality

    public abstract bool Equals(ComputedInput? other);
    public override bool Equals(object? obj)
        => obj is ComputedInput other && Equals(other);

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode;

    public static bool operator ==(ComputedInput? left, ComputedInput? right)
        => Equals(left, right);
    public static bool operator !=(ComputedInput? left, ComputedInput? right)
        => !Equals(left, right);

    // Protected methods

    protected virtual Type GetDisposedType()
        => GetType();
}
