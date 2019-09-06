namespace Stl
{
    public static class StringEx
    {
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
    }
}
