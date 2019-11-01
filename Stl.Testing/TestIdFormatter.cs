using System;
using System.Text;
using Stl.IO;

namespace Stl.Testing 
{
    public class TestIdFormatter
    {
        public static readonly string RunId = Guid.NewGuid().ToString("N");

        public string TestId { get; set; }
        public int MaxLength { get; set; } = 12; 

        public TestIdFormatter(Type testType) : this($"{testType.Name}_{testType.Namespace}") { }
        public TestIdFormatter(string testId) => TestId = testId;

        public override string? ToString() => Format();
        public static implicit operator string(TestIdFormatter f) => f.ToString();

        public string Format(
            bool withMachineId = true,
            bool withTestId = true,
            bool withRunId = true,
            int? maxLength = null)
        {
            var sb = new StringBuilder();
            if (withMachineId)
                sb.Append(Environment.MachineName ?? "unknown").Append("_");
            if (withTestId)
                sb.Append(TestId).Append("_");
            if (withRunId)
                sb.Append(RunId).Append("_");
            if (sb.Length > 0)
                sb.Length -= 1;
            var r = sb.ToString();
            r = PathEx.GetHashedName(r, null, maxLength ?? MaxLength);
            r = PostProcess(r);
            return r;
        }

        protected virtual string PostProcess(string id) => id;
    }

    public class AzureTestIdFormatter : TestIdFormatter
    {
        public AzureTestIdFormatter(Type testType) : base(testType) { }
        public AzureTestIdFormatter(string testId) : base(testId) { }

        protected override string PostProcess(string id) => id.Replace('_', '-');
    }
}
