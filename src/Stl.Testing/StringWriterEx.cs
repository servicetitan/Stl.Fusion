using System.IO;

namespace Stl.Testing
{
    public static class StringWriterEx
    {
        public static void Clear(this StringWriter writer)
        {
            writer.Flush();
            writer.GetStringBuilder().Clear();
        }

        public static string GetContent(this StringWriter writer)
        {
            writer.Flush();
            return writer.GetStringBuilder().ToString();
        }

        public static string GetContentAndClear(this StringWriter writer)
        {
            var content = writer.GetContent();
            writer.Clear();
            return content;
        }
    }
}
