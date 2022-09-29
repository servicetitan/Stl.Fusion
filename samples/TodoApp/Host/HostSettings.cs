namespace Templates.TodoApp.Host;

public class HostSettings
{
    public bool UseMultitenancy { get; set; } = true;
    public bool UseInMemoryAuthService { get; set; } = false;

    // DBs
    public string UseSqlServer { get; set; } = "";
        // "Data Source=localhost;Initial Catalog=fusion_blazorise_template;Integrated Security=False;User ID=sa;Password=SqlServer1";
    public string UsePostgreSql { get; set; } =
        "Server=localhost;Database=stl_fusion_todoapp_{0:StorageId};Port=5432;User Id=postgres;Password=postgres";

    public string MicrosoftAccountClientId { get; set; } = "6839dbf7-d1d3-4eb2-a7e1-ce8d48f34d00";
    public string MicrosoftAccountClientSecret { get; set; } =
        Encoding.UTF8.GetString(Convert.FromBase64String(
            "REFYeH4yNTNfcVNWX2h0WkVoc1V6NHIueDN+LWRxUTA2Zw=="));

    public string GitHubClientId { get; set; } = "7a38bc415f7e1200fee2";
    public string GitHubClientSecret { get; set; } =
        Encoding.UTF8.GetString(Convert.FromBase64String(
            "OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));
}
