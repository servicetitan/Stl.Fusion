using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Templates.TodoApp.Host;
using Templates.TodoApp.Services;

var host = Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(cfg => {
        // Looks like there is no better way to set _default_ URL
        cfg.Sources.Insert(0, new MemoryConfigurationSource() {
            InitialData = new Dictionary<string, string>() {
                {WebHostDefaults.ServerUrlsKey, "http://localhost:5005"},
            }
        });
    })
    .ConfigureWebHostDefaults(webHost => webHost
        .UseDefaultServiceProvider((ctx, options) => {
            if (ctx.HostingEnvironment.IsDevelopment()) {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }
        })
        .UseStartup<Startup>())
    .Build();

// Ensure the DB is created
var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
await using var dbContext = dbContextFactory.CreateDbContext();
await dbContext.Database.EnsureDeletedAsync();
await dbContext.Database.EnsureCreatedAsync();

await host.RunAsync();
