
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BazanAI.Weather.Workers;

public class WeatherPollingWorker : BackgroundService
{
    private readonly ILogger<WeatherPollingWorker> _logger;

    public WeatherPollingWorker(ILogger<WeatherPollingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling weather data at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromHours(3), stoppingToken);
        }
    }
}
