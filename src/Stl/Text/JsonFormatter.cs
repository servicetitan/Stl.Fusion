namespace Stl.Text;

public static class JsonFormatter
{
    public static ITextSerializer Serializer { get; set; } =
        new SystemJsonSerializer(new() {
            WriteIndented = true
        });

    public static string Format(object value)
        => Serializer.Write(value);
}
