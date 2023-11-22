using Stl.IO;

namespace Stl.Testing;

public class TestIdFormatter(string testId)
{
    public static readonly string RunId = Guid.NewGuid().ToString("N");

    public string MachineId { get; set; } = Environment.MachineName.ToLowerInvariant() ?? "unknown";
    public string TestId { get; set; } = testId;
    public int MaxLength { get; set; } = 12;
    public bool AlwaysHash { get; set; } = false;

    public TestIdFormatter(Type testType)
        : this($"{testType.Name}_{testType.Namespace}")
    { }

    public override string ToString() => Format();
    public static implicit operator string(TestIdFormatter f) => f.Format();

    public string Format(
        bool withMachineId = true,
        bool withTestId = true,
        bool withRunId = true,
        int? maxLength = null,
        bool? alwaysHash = null)
    {
        var sb = StringBuilderExt.Acquire();
        if (withMachineId) {
            sb.Append(MachineId);
            sb.Append('_');
        }
        if (withTestId) {
            sb.Append(TestId);
            sb.Append('_');
        }
        if (withRunId) {
            sb.Append(RunId);
            sb.Append('_');
        }
        var r = sb.ToStringAndRelease()[.. Math.Max(0, sb.Length - 1)];
        r = FilePath.GetHashedName(r, null, maxLength ?? MaxLength, alwaysHash ?? AlwaysHash);
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
