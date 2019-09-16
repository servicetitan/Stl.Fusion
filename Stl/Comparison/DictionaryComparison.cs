using System.Collections.Generic;

namespace Stl.Comparison
{
    public static class DictionaryComparison
    {
        public static DictionaryComparison<TKey, TValue> New<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> left, 
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TValue>? valueComparer = null)
            where TKey : notnull
            => new DictionaryComparison<TKey, TValue>(left, right, valueComparer);
    }

    public class DictionaryComparison<TKey, TValue>
        where TKey : notnull
    {
        public IReadOnlyDictionary<TKey, TValue> Left { get; }
        public IReadOnlyDictionary<TKey, TValue> Right { get; }
        public IEqualityComparer<TValue> ValueComparer { get; }

        public List<KeyValuePair<TKey, TValue>> LeftOnly { get; } = new List<KeyValuePair<TKey, TValue>>();
        public List<KeyValuePair<TKey, TValue>> RightOnly { get; } = new List<KeyValuePair<TKey, TValue>>();
        public List<KeyValuePair<TKey, TValue>> SharedEqual { get; } = new List<KeyValuePair<TKey, TValue>>();
        public List<(TKey Key, TValue LeftValue, TValue RightValue)> SharedUnequal { get; } = 
            new List<(TKey Key, TValue LeftValue, TValue RightValue)>();

        public bool AreCountsEqual => Left.Count == Right.Count;
        public bool AreEqual => SharedEqual.Count == Left.Count && AreCountsEqual;

        public DictionaryComparison(
            IReadOnlyDictionary<TKey, TValue> left, 
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TValue>? valueComparer = null)
        {
            Left = left;
            Right = right;
            ValueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            Compare();
        }

        private void Compare()
        {
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
