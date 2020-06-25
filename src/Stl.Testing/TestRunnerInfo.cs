using System;

namespace Stl.Testing
{
    public static class TestRunnerInfo
    {
        public static class TeamCity
        {
            public static readonly Version? Version;
            public static readonly string ProjectName;
            public static readonly string BuildConfigurationName;

            static TeamCity()
            {
                var version = Environment.GetEnvironmentVariable("TEAMCITY_VERSION");
                if (!string.IsNullOrEmpty(version))
                    Version.TryParse(version, out Version);
                ProjectName = Environment.GetEnvironmentVariable("TEAMCITY_PROJECT_NAME") ?? "";
                BuildConfigurationName = Environment.GetEnvironmentVariable("TEAMCITY_BUILDCONF_NAME") ?? "";
            }
        }

        public static bool IsBuildAgent() 
            => TeamCity.Version != null;
    }
}
