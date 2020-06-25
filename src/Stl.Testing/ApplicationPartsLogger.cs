using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Testing
{
    public class ApplicationPartsLogger : IHostedService
    {
        private readonly ILogger _log;
        private readonly ApplicationPartManager _partManager;

        public ApplicationPartsLogger(
            ApplicationPartManager partManager, 
            ILogger<ApplicationPartsLogger>? log = null)
        {
            _log = log ??= NullLogger<ApplicationPartsLogger>.Instance;
            _partManager = partManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var applicationParts = _partManager.ApplicationParts.Select(x => x.Name);
            var controllerFeature = new ControllerFeature();
            _partManager.PopulateFeature(controllerFeature);
            var controllers = controllerFeature.Controllers.Select(x => x.Name);

            _log.LogInformation($"Application parts: {string.Join(", ", applicationParts)}");
            _log.LogInformation($"Controllers: {string.Join(", ", controllers)}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) 
            => Task.CompletedTask;
    }
}
