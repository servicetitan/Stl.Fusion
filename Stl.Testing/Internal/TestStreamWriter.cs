using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Text;
using FluentAssertions.Primitives;

namespace Stl.Testing.Internal
{
    public class TestStreamWriter : IStandardStreamWriter
    {
        protected object Lock = new object();
        public StringBuilder StringBuilder = new StringBuilder();
        public TextWriter? TextWriter { get; set; }
        
        public override string ToString()
        {
            lock (Lock) {
                return StringBuilder.ToString();
            }
        }

        public void Write(char value)
        {
            lock (Lock) {
                StringBuilder.Append(value);
                TextWriter?.Write(value);
            }
        }

        public void Write(string value)
        {
            lock (Lock) {
                StringBuilder.Append(value);
                TextWriter?.Write(value);
            }
        }

        public TestStreamWriter Clear()
        {
            lock (Lock) {
                StringBuilder.Clear();
                return this;
            }
        }

        public StringAssertions Should() => new StringAssertions(ToString());
    }
}
