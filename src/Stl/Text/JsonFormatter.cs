namespace Stl.Text;

public static class JsonFormatter
{
    public static string Format(object value)
        => SystemJsonSerializer.Readable.Write(value);
}
