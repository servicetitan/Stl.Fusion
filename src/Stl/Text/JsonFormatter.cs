namespace Stl.Text;

public static class JsonFormatter
{
    public static IUtf16Writer Formatter { get; set; } =
        new SystemJsonSerializer(new() {
            WriteIndented = true
        });

    public static string Format(object value)
        => Formatter.Write(value);
}
