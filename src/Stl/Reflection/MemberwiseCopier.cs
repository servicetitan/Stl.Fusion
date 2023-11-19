using System.Diagnostics.CodeAnalysis;

namespace Stl.Reflection;

public record MemberwiseCopier<[
    DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
{
    public static readonly MemberwiseCopier<T> Default = new();
    private const BindingFlags PropertyOrFieldBindingFlagsMask =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public Type Type => typeof(T);
    public BindingFlags PropertyBindingFlags { get; init; } = BindingFlags.Instance | BindingFlags.Public;
    public BindingFlags FieldBindingFlags { get; init; } = 0;
    public Func<MemberInfo, bool>? Filter { get; init; }

    public MemberwiseCopier<T> WithProperties(BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        => this with { PropertyBindingFlags = PropertyBindingFlags | (bindingFlags & PropertyOrFieldBindingFlagsMask) };
    public MemberwiseCopier<T> WithFields(BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        => this with { FieldBindingFlags = FieldBindingFlags | (bindingFlags & PropertyOrFieldBindingFlagsMask) };
    public MemberwiseCopier<T> WithFilter(Func<MemberInfo, bool>? filter)
        => this with { Filter = filter };

    public T Invoke(T source, T target)
    {
        var oSource = (object) source!;
        var oTarget = (object) target!;
#pragma warning disable IL2075
        var fields = Type.GetFields(FieldBindingFlags & PropertyOrFieldBindingFlagsMask);
#pragma warning restore IL2075
        foreach (var field in fields) {
            if (!(Filter?.Invoke(field) ?? true))
                continue;
            field.SetValue(oTarget, field.GetValue(oSource));
        }
#pragma warning disable IL2075
        var properties = Type.GetProperties(PropertyBindingFlags & PropertyOrFieldBindingFlagsMask);
#pragma warning restore IL2075
        foreach (var property in properties) {
            if (!(Filter?.Invoke(property) ?? true))
                continue;
            property.SetValue(oTarget, property.GetValue(oSource));
        }
        return target;
    }
}

public static class MemberwiseCopier
{
    public static T Invoke<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (T source, T target, Func<MemberwiseCopier<T>, MemberwiseCopier<T>>? configurator = null)
    {
        var copier = MemberwiseCopier<T>.Default;
        if (configurator != null)
            copier = configurator(MemberwiseCopier<T>.Default);
        return copier.Invoke(source, target);
    }
}
