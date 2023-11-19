// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0

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

#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP3_1

/// <summary>
/// Specifies the types of members that are dynamically accessed.
///
/// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a
/// bitwise combination of its member values.
/// </summary>
[Flags]
#pragma warning disable MA0062
#pragma warning disable CA2217
public enum DynamicallyAccessedMemberTypes
#pragma warning restore CA2217
#pragma warning restore MA0062
{
    /// <summary>
    /// Specifies no members.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies the default, parameterless public constructor.
    /// </summary>
    PublicParameterlessConstructor = 0x0001,

    /// <summary>
    /// Specifies all public constructors.
    /// </summary>
    PublicConstructors = 0x0002 | PublicParameterlessConstructor,

    /// <summary>
    /// Specifies all non-public constructors.
    /// </summary>
    NonPublicConstructors = 0x0004,

    /// <summary>
    /// Specifies all public methods.
    /// </summary>
    PublicMethods = 0x0008,

    /// <summary>
    /// Specifies all non-public methods.
    /// </summary>
    NonPublicMethods = 0x0010,

    /// <summary>
    /// Specifies all public fields.
    /// </summary>
    PublicFields = 0x0020,

    /// <summary>
    /// Specifies all non-public fields.
    /// </summary>
    NonPublicFields = 0x0040,

    /// <summary>
    /// Specifies all public nested types.
    /// </summary>
    PublicNestedTypes = 0x0080,

    /// <summary>
    /// Specifies all non-public nested types.
    /// </summary>
    NonPublicNestedTypes = 0x0100,

    /// <summary>
    /// Specifies all public properties.
    /// </summary>
    PublicProperties = 0x0200,

    /// <summary>
    /// Specifies all non-public properties.
    /// </summary>
    NonPublicProperties = 0x0400,

    /// <summary>
    /// Specifies all public events.
    /// </summary>
    PublicEvents = 0x0800,

    /// <summary>
    /// Specifies all non-public events.
    /// </summary>
    NonPublicEvents = 0x1000,

    /// <summary>
    /// Specifies all interfaces implemented by the type.
    /// </summary>
    Interfaces = 0x2000,

    /// <summary>
    /// Specifies all members.
    /// </summary>
    All = ~None
}

/// <summary>
/// Indicates that the specified method requires dynamic access to code that is not referenced
/// statically, for example through <see cref="Reflection"/>.
/// </summary>
/// <remarks>
/// This allows tools to understand which methods are unsafe to call when removing unreferenced
/// code from an application.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
public sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">
    /// A message that contains information about the usage of unreferenced code.
    /// </param>
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets a message that contains information about the usage of unreferenced code.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets an optional URL that contains more information about the method,
    /// why it requires unreferenced code, and what options a consumer has to deal with it.
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Indicates that certain members on a specified <see cref="Type"/> are accessed dynamically,
/// for example through <see cref="System.Reflection"/>.
/// </summary>
/// <remarks>
/// This allows tools to understand which members are being accessed during the execution
/// of a program.
///
/// This attribute is valid on members whose type is <see cref="Type"/> or <see cref="string"/>.
///
/// When this attribute is applied to a location of type <see cref="string"/>, the assumption is
/// that the string represents a fully qualified type name.
///
/// When this attribute is applied to a class, interface, or struct, the members specified
/// can be accessed dynamically on <see cref="Type"/> instances returned from calling
/// <see cref="object.GetType"/> on instances of that class, interface, or struct.
///
/// If the attribute is applied to a method it's treated as a special case and it implies
/// the attribute should be applied to the "this" parameter of the method. As such the attribute
/// should only be used on instance methods of types assignable to System.Type (or string, but no methods
/// will use it there).
/// </remarks>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false)]
public sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class
    /// with the specified member types.
    /// </summary>
    /// <param name="memberTypes">The types of members dynamically accessed.</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    /// <summary>
    /// Gets the <see cref="DynamicallyAccessedMemberTypes"/> which specifies the type
    /// of members dynamically accessed.
    /// </summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#endif
