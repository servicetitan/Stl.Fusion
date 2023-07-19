namespace Stl.Fusion.Tests;

public enum FusionTestDbType
{
    Sqlite = 0,
    PostgreSql = 1,
    MariaDb = 2,
    SqlServer = 3,
    InMemory = 4,
}

public static class FusionTestDbTypeExt
{
    public static bool IsAvailable(this FusionTestDbType dbType)
        => TestRunnerInfo.IsBuildAgent()
            ? dbType.IsAvailableOnBuildAgent()
            : dbType.IsAvailableLocally();

    public static bool IsAvailableLocally(this FusionTestDbType dbType)
        => dbType is FusionTestDbType.InMemory or FusionTestDbType.Sqlite or FusionTestDbType.PostgreSql;

    public static bool IsAvailableOnBuildAgent(this FusionTestDbType dbType)
        => dbType is FusionTestDbType.InMemory;
}
