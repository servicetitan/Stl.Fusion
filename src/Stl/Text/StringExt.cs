namespace Stl.Text;

public static class StringExt
{
    public static string ManyToOne(IEnumerable<string> values) => ManyToOne(values, ListFormat.Default);
    public static string ManyToOne(IEnumerable<string> values, ListFormat listFormat)
    {
        using var f = listFormat.CreateFormatter();
        foreach (var value in values)
            f.Append(value);
        f.AppendEnd();
        return f.OutputBuilder.ToString();
    }

    public static string[] OneToMany(string value) => OneToMany(value, ListFormat.Default);
    public static string[] OneToMany(string value, ListFormat listFormat)
    {
        if (value == "")
            return Array.Empty<string>();
        using var p = listFormat.CreateParser(value);
        var buffer = MemoryBuffer<string>.Lease(true);
        try {
            while (p.TryParseNext())
                buffer.Add(p.Item);
            return buffer.ToArray();
        }
        finally {
            buffer.Release();
        }
    }

    public static int GetDeterministicHashCode(this string source)
    {
        unchecked {
            var hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;
            for (var i = 0; i < source.Length; i += 2) {
                hash1 = ((hash1 << 5) + hash1) ^ source[i];
                if (i == source.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ source[i + 1];
            }
            return hash1 + (hash2 * 1566083941);
        }
    }

    public static string TrimSuffix(this string source, params string[] suffixes)
    {
        foreach (var suffix in suffixes) {
            var result = source.TrimSuffix(suffix);
            if (!ReferenceEquals(result, source))
                return result;
        }
        return source;
    }

    public static string TrimSuffix(this string source, string suffix)
        => source.EndsWith(suffix) ? source.Substring(0, source.Length - suffix.Length) : source;
}
