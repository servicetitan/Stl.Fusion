#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
///     Specifies that when a method returns
///     <see cref="P:System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.ReturnValue" />, the parameter may be
///     <see langword="null" /> even if the corresponding type disallows it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter may
    ///     be <see langword="null" />.
    /// </param>
    public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    /// <summary>Gets the return value condition.</summary>
    /// <returns>
    ///     The return value condition. If the method returns this value, the associated parameter may be
    ///     <see langword="null" />.
    /// </returns>
    public bool ReturnValue { get; }
}

/// <summary>Specifies that an output may be <see langword="null" /> even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter |
    AttributeTargets.ReturnValue)]
public sealed class MaybeNullAttribute : Attribute;

#endif
