using System.Collections.Generic;
using Stl.Collections;

namespace Stl.Comparison
{
    public static class DictionaryComparison
    {
        public static DictionaryComparison<TKey, TValue> New<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> left,
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TValue>? valueComparer = null)
            where TKey : notnull
            => new(left, right, valueComparer);
    }

    public class DictionaryComparison<TKey, TValue>
        where TKey : notnull
    {
        public IReadOnlyDictionary<TKey, TValue> Left { get; }
        public IReadOnlyDictionary<TKey, TValue> Right { get; }
        public IEqualityComparer<TValue> ValueComparer { get; }

        public List<KeyValuePair<TKey, TValue>> LeftOnly { get; } = new();
        public List<KeyValuePair<TKey, TValue>> RightOnly { get; } = new();
        public List<KeyValuePair<TKey, TValue>> SharedEqual { get; } = new();
        public List<(TKey Key, TValue LeftValue, TValue RightValue)> SharedUnequal { get; } =
            new List<(TKey Key, TValue LeftValue, TValue RightValue)>();

        public bool AreCountsEqual => Left.Count == Right.Count;
        public bool AreEqual => SharedEqual.Count == Left.Count && AreCountsEqual;

        public DictionaryComparison(
            IEnumerable<(TKey, TValue)> left,
            IEnumerable<(TKey, TValue)> right,
            IEqualityComparer<TValue>? valueComparer = null)
            : this(left.ToDictionary(), right.ToDictionary(), valueComparer) { }

        public DictionaryComparison(
            IEnumerable<KeyValuePair<TKey, TValue>> left,
            IEnumerable<KeyValuePair<TKey, TValue>> right,
            IEqualityComparer<TValue>? valueComparer = null)
            : this(left.ToDictionary(), right.ToDictionary(), valueComparer) { }

        public DictionaryComparison(
            IReadOnlyDictionary<TKey, TValue> left,
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TValue>? valueComparer = null)
        {
            Left = left;
            Right = right;
            ValueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var lPair in Left) {
                if (Right.TryGetValue(lPair.Key, out var rValue)) {
                    if (ValueComparer.Equals(lPair.Value, rValue))
                        SharedEqual.Add(lPair);
                    else
                        SharedUnequal.Add((lPair.Key, lPair.Value, rValue));
                }
                else {
                    LeftOnly.Add(lPair);
                }
            }
            foreach (var rPair in Right) {
                if (Left.ContainsKey(rPair.Key))
                    continue;
                RightOnly.Add(rPair);
            }
        }
    }
}
