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
    public static bool IsUsed(this FusionTestDbType dbType)
        => TestRunnerInfo.IsBuildAgent()
            ? dbType.IsUsedOnBuildAgent()
            : dbType.IsUsedLocally();

    public static bool IsUsedLocally(this FusionTestDbType dbType)
        => dbType is FusionTestDbType.InMemory or FusionTestDbType.Sqlite or FusionTestDbType.PostgreSql;

    public static bool IsUsedOnBuildAgent(this FusionTestDbType dbType)
        => dbType is FusionTestDbType.InMemory;
}
