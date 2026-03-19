
using System;
using Microsoft.Extensions.Logging;

namespace BazanAI.Scheduler.Jobs;

public class DailyPriceSyncJob
{
    private readonly ILogger<DailyPriceSyncJob> _logger;

    public DailyPriceSyncJob(ILogger<DailyPriceSyncJob> logger)
    {
        _logger = logger;
    }

    public void Execute()
    {
        _logger.LogInformation("Executing Daily Price Sync Job at {time}", DateTimeOffset.Now);
    }
}
