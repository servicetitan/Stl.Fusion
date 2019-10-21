using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Stl.CommandLine 
{
    [Serializable]
    public class CliList<T> : List<T>, IEnumerable<IFormattable>
    {
        public static readonly string DefaultItemTemplate = "{0}";
        public string ItemTemplate { get; set; } = DefaultItemTemplate;

        public CliList() { }
        public CliList(IEnumerable<T> collection) : base(collection) { }

        public new IEnumerator<IFormattable> GetEnumerator() 
            => (this as IEnumerable<T>)
                // ReSharper disable once HeapView.BoxingAllocation
                .Select(x => CliString.New(
                    string.Format(CultureInfo.InvariantCulture, ItemTemplate, x)
                ) as IFormattable)
                .GetEnumerator();
    }
}
