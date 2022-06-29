namespace Stl.Text;

public sealed class ReflectionFormatProvider : IFormatProvider, ICustomFormatter {
    public object? GetFormat(Type? formatType)
        => formatType == typeof(ICustomFormatter) ? this : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider) {
        var formats = (format ?? string.Empty).Split(new [] { ':' }, 2);
        var propertyName = formats[0].TrimEnd('}');
        var suffix = formats[0][propertyName.Length..];
        var propertyFormat = formats.Length > 1 ? formats[1] : null;

        var getter = arg?.GetType().GetProperty(propertyName)?.GetGetMethod();
        if (getter == null)
            return arg is IFormattable formattable
                ? formattable.ToString(format, formatProvider)
                : arg?.ToString() ?? "";

        var value = getter.Invoke(arg, null);
        return propertyFormat == null
            ? (value?.ToString() ?? "") + suffix
#pragma warning disable MA0011
            : string.Format($"{{0:{propertyFormat}}}", value);
#pragma warning restore MA0011
    }
}
