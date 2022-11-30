namespace Stl.Fusion.Blazor;

public sealed class DefaultParameterComparer : ParameterComparer
{
    public static DefaultParameterComparer Instance { get; } = new();

    // Mostly copied from Microsoft.AspNetCore.Components.ChangeDetection
    public override bool AreEqual(object? oldValue, object? newValue)
    {
        var oldIsNotNull = oldValue != null;
        var newIsNotNull = newValue != null;
        if (oldIsNotNull != newIsNotNull)
            return false; // One's null and the other isn't, so different
        if (oldIsNotNull) {
            // Both are not null (considering previous check)
            var oldValueType = oldValue!.GetType();
            var newValueType = newValue!.GetType();
            if (oldValueType != newValueType            // Definitely different
                || !IsKnownImmutableType(oldValueType)  // Maybe different
                || !oldValue.Equals(newValue))          // Somebody says they are different
                return false;
        }

        // By now we know either both are null, or they are the same immutable type
        // and ThatType::Equals says the two values are equal.
        return true;
    }

    // The contents of this list need to trade off false negatives against computation
    // time. So we don't want a huge list of types to check (or would have to move to
    // a hashtable lookup, which is differently expensive). It's better not to include
    // uncommon types here even if they are known to be immutable.
    private static bool IsKnownImmutableType(Type type)
        => type.IsPrimitive
            || type == typeof(string)
            || type == typeof(DateTime)
            || type == typeof(Type)
            || type == typeof(decimal)
            || type == typeof(Guid);
}
