using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Templates.Blazor2.Services;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Templates.Blazor2.Host
{
    public class Startup
    {
        private IConfiguration Cfg { get; }
        private IWebHostEnvironment Env { get; }
        private ILogger Log { get; set; } = NullLogger<Startup>.Instance;

        public Startup(IConfiguration cfg, IWebHostEnvironment environment)
        {
            Cfg = cfg;
            Env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(opts => {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            // Logging
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            });

            // DbContext & related services
            /*
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            var dbPath = appTempDir & "App.db";
            services.AddDbContextFactory<AppDbContext>(builder => {
                builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
            });
            */

            // Fusion services
            services.AddSingleton(c => {
                var serverSettings = c.GetRequiredService<ServerSettings>();
                return new Publisher.Options() { Id = serverSettings.PublisherId };
            });
            services.AddSingleton(new PresenceService.Options() { UpdatePeriod = TimeSpan.FromMinutes(1) });
            var fusion = services.AddFusion();
            var fusionServer = fusion.AddWebServer();
            var fusionClient = fusion.AddRestEaseClient();
            var fusionAuth = fusion.AddAuthentication().AddServer(
                signInControllerOptionsBuilder: (_, options) => {
                    options.DefaultScheme = GoogleDefaults.AuthenticationScheme;
                },
                authHelperOptionsBuilder: (_, options) => {
                    options.NameClaimKeys = Array.Empty<string>();
                });

            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]
            services.AttributeScanner()
                .AddServicesFrom(typeof(TimeService).Assembly)
                .AddServicesFrom(Assembly.GetExecutingAssembly());
            // Registering shared services from the client
            UI.Program.ConfigureSharedServices(services);

            services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options => {
                options.LoginPath = "/signIn";
                options.LogoutPath = "/signOut";
            }).AddGoogle(options => {
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            }).AddMicrosoftAccount(options => {
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            });
            services.Configure<GoogleOptions>((c, name, options) => {
                var serverSettings = c.GetRequiredService<ServerSettings>();
                options.ClientId = serverSettings.GoogleClientId;
                options.ClientSecret = serverSettings.GoogleClientSecret;
            });
            services.Configure<MicrosoftAccountOptions>((c, name, options) => {
                var serverSettings = c.GetRequiredService<ServerSettings>();
                options.ClientId = serverSettings.MicrosoftAccountClientId;
                options.ClientSecret = serverSettings.MicrosoftAccountClientSecret;
            });

            // Web
            services.AddRouting();
            services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly());
            services.AddServerSideBlazor(o => o.DetailedErrors = true);
            fusionAuth.AddBlazor(o => { }); // Must follow services.AddServerSideBlazor()!

            // Swagger & debug tools
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "Templates.Blazor2 API", Version = "v1"
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
                wwwRootPath = Path.GetFullPath(Path.Combine(baseDir, $"../../UI/net5.0/wwwroot"));
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
            app.ApplicationServices.UseBootstrapProviders().UseFontAwesomeIcons(); // Blazorise
            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFusionWebSocketServer();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
