
using MQTTnet.Server;
using MQTTnet;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BazanAI.IoTIngestion.Mqtt;

public class MqttBrokerService : BackgroundService
{
    private readonly ILogger<MqttBrokerService> _logger;

    public MqttBrokerService(ILogger<MqttBrokerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttFactory();
        var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
        var server = mqttFactory.CreateMqttServer(mqttServerOptions);
        await server.StartAsync();
        _logger.LogInformation("MQTT Server started.");
    }
}
