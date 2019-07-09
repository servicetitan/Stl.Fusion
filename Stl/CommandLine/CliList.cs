using System;
using System.Collections.Generic;
using System.Linq;

namespace Stl.CommandLine 
{
    [Serializable]
    public class CliList<T> : List<T>, IEnumerable<IFormattable>
        where T: IFormattable
    {
        public CliList() { }
        public CliList(IEnumerable<T> collection) : base(collection) { }

        public IEnumerator<IFormattable> GetEnumerator() 
            => (this as IEnumerable<T>).Select(x => x as IFormattable).GetEnumerator();
    }
}
