using Stl.Fusion.Internal;

namespace Stl.Fusion;

public abstract class ComputedInput : IEquatable<ComputedInput>
{
    public IFunction Function { get; private set; } = null!;
    public int HashCode { get; private set; }
    public virtual string Category {
        get => Function.ToString() ?? "";
        init => throw Errors.ComputedInputCategoryCannotBeSet();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Initialize(IFunction function, int hashCode)
    {
        Function = function;
        HashCode = hashCode;
    }

    public override string ToString()
        => $"{Category} #{HashCode}";

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
}
