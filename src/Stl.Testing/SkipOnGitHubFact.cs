using Xunit;

namespace Stl.Testing;

public sealed class SkipOnGitHubFact : FactAttribute
{
    public SkipOnGitHubFact() {
        if (TestRunnerInfo.GitHub.IsActionRunning)
            Skip = "Ignored on GitHub";
    }
}
