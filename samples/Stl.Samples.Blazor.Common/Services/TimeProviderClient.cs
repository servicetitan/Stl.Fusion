using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Samples.Blazor.Common.Services
{
    public class TimeProviderClient : ITimeProvider
    {
        private readonly ILogger<TimeProviderClient> _log;
        private readonly HttpClient _httpClient;

        public TimeProviderClient(
            HttpClient httpClient,
            ILogger<TimeProviderClient>? log = null)
        {
            _httpClient = httpClient; 
            _log = log ??= NullLogger<TimeProviderClient>.Instance;
        }

        public virtual async Task<DateTime> GetTimeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
