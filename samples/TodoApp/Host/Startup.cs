using System.Data;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Templates.TodoApp.Services;
using Stl.DependencyInjection;
using Stl.Fusion.Blazor;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Extensions;
using Stl.Fusion.Operations.Reprocessing;
using Stl.IO;
using Templates.TodoApp.Abstractions;
using Templates.TodoApp.UI;

namespace Templates.TodoApp.Host;

public class Startup
{
    private IConfiguration Cfg { get; }
    private IWebHostEnvironment Env { get; }
    private HostSettings HostSettings { get; set; } = null!;
    private ILogger Log { get; set; } = NullLogger<Startup>.Instance;

    public Startup(IConfiguration cfg, IWebHostEnvironment environment)
    {
        Cfg = cfg;
        Env = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            if (Env.IsDevelopment()) {
                logging.AddFilter(typeof(App).Namespace, LogLevel.Information);
                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
                logging.AddFilter("Stl.Fusion.Operations", LogLevel.Information);
            }
        });

        services.AddSettings<HostSettings>();
#pragma warning disable ASP0000
        HostSettings = services.BuildServiceProvider().GetRequiredService<HostSettings>();
#pragma warning restore ASP0000

        // DbContext & related services
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        var dbPath = appTempDir & "App.db";
        services.AddDbContextFactory<AppDbContext>(dbContext => {
            if (!string.IsNullOrEmpty(HostSettings.UseSqlServer))
                dbContext.UseSqlServer(HostSettings.UseSqlServer);
            else if (!string.IsNullOrEmpty(HostSettings.UsePostgreSql)) {
                dbContext.UseNpgsql(HostSettings.UsePostgreSql);
                // dbContext.UseNpgsqlHintFormatter();
            }
            else
                dbContext.UseSqlite($"Data Source={dbPath}");
            if (Env.IsDevelopment())
                dbContext.EnableSensitiveDataLogging();
        });
        services.AddTransient(c => new DbOperationScope<AppDbContext>(c) {
            IsolationLevel = IsolationLevel.Serializable,
        });
        services.AddDbContextServices<AppDbContext>(dbContext => {
            // This is the best way to add DbContext-related services from Stl.Fusion.EntityFramework
            dbContext.AddOperations((_, o) => {
                // We use FileBasedDbOperationLogChangeMonitor, so unconditional wake up period
                // can be arbitrary long - all depends on the reliability of Notifier-Monitor chain.
                o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(Env.IsDevelopment() ? 60 : 5);
            });
            var operationLogChangeAlertPath = dbPath + "_changed";
            dbContext.AddFileBasedOperationLogChangeTracking(operationLogChangeAlertPath);
            // dbContext.AddRedisDb("localhost", "Fusion.Samples.TodoApp");
            // dbContext.AddRedisOperationLogChangeTracking();
            if (!HostSettings.UseInMemoryAuthService)
                dbContext.AddAuthentication<string>();
            dbContext.AddKeyValueStore();
        });

        // Fusion services
        services.AddSingleton(new Publisher.Options() { Id = HostSettings.PublisherId });
        var fusion = services.AddFusion();
        var fusionServer = fusion.AddWebServer();
        var fusionClient = fusion.AddRestEaseClient();
        var fusionAuth = fusion.AddAuthentication().AddServer(
            signInControllerOptionsBuilder: (_, options) => {
                options.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
            },
            authHelperOptionsBuilder: (_, options) => {
                options.NameClaimKeys = Array.Empty<string>();
            });
        fusion.AddSandboxedKeyValueStore();
        fusion.AddOperationReprocessor();
        // You don't need to manually add TransientFailureDetector -
        // it's here only to show that operation reprocessor works
        // when TodoService.AddOrUpdate throws this exception.
        // Database-related transient errors are auto-detected by
        // DbOperationScopeProvider<TDbContext> (it uses DbContext's
        // IExecutionStrategy to do this).
        services.TryAddEnumerable(ServiceDescriptor.Singleton(
            TransientFailureDetector.New(e => e is DbUpdateConcurrencyException)));

        // Compute service(s)
        fusion.AddComputeService<ITodoService, TodoService>();

        // Shared services
        StartupHelper.ConfigureSharedServices(services);

        // ASP.NET Core authentication providers
        services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options => {
            options.LoginPath = "/signIn";
            options.LogoutPath = "/signOut";
            if (Env.IsDevelopment())
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        }).AddMicrosoftAccount(options => {
            options.ClientId = HostSettings.MicrosoftAccountClientId;
            options.ClientSecret = HostSettings.MicrosoftAccountClientSecret;
            // That's for personal account authentication flow
            options.AuthorizationEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
            options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        }).AddGitHub(options => {
            options.ClientId = HostSettings.GitHubClientId;
            options.ClientSecret = HostSettings.GitHubClientSecret;
            options.Scope.Add("read:user");
            options.Scope.Add("user:email");
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        });

        // Web
        services.AddRouting();
        services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly());
        services.AddServerSideBlazor(o => o.DetailedErrors = true);
        fusionAuth.AddBlazor(o => { }); // Must follow services.AddServerSideBlazor()!

        // Swagger & debug tools
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "Templates.TodoApp API", Version = "v1"
            });
        });
    }

    public void Configure(IApplicationBuilder app, ILogger<Startup> log)
    {
        Log = log;

        // This server serves static content from Blazor Client,
        // and since we don't copy it to local wwwroot,
        // we need to find Client's wwwroot in bin/(Debug/Release) folder
        // and set it as this server's content root.
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        var wwwRootPath = Path.Combine(baseDir, "wwwroot");
        if (!Directory.Exists(Path.Combine(wwwRootPath, "_framework")))
            // This is a regular build, not a build produced w/ "publish",
            // so we remap wwwroot to the client's wwwroot folder
            wwwRootPath = Path.GetFullPath(Path.Combine(baseDir, "../../UI/net6.0/wwwroot"));
        Env.WebRootPath = wwwRootPath;
        Env.WebRootFileProvider = new PhysicalFileProvider(Env.WebRootPath);
        StaticWebAssetsLoader.UseStaticWebAssets(Env, Cfg);

        if (Env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        app.UseWebSockets(new WebSocketOptions() {
            KeepAliveInterval = TimeSpan.FromSeconds(30),
        });
        app.UseFusionSession();

        // Static + Swagger
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseSwagger();
        app.UseSwaggerUI(c => {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        });

        // API controllers
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => {
            endpoints.MapBlazorHub();
            endpoints.MapFusionWebSocketServer();
            endpoints.MapControllers();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
