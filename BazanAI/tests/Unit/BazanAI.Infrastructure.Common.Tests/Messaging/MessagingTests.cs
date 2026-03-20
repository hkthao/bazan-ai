
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Infrastructure.Common;
using BazanAI.Infrastructure.Common.Messaging;
using BazanAI.SharedKernel.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace BazanAI.Infrastructure.Common.Tests.Messaging;

public class MessagingTests
{
    [Fact]
    public void AddBazanMassTransit_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        // Act
        services.AddBazanMassTransit(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IEventPublisher>().Should().BeOfType<RabbitMqPublisher>();
        serviceProvider.GetService<IBusControl>().Should().NotBeNull();
    }

    [Fact]
    public async Task RabbitMqPublisher_ShouldCallPublishEndpoint()
    {
        // Arrange
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var publisher = new RabbitMqPublisher(publishEndpoint);
        var testEvent = new TestEvent { Id = 1 };

        // Act
        await publisher.PublishAsync(testEvent);

        // Assert
        await publishEndpoint.Received(1).Publish(testEvent, Arg.Any<CancellationToken>());
    }

    public class TestEvent
    {
        public int Id { get; set; }
    }
}
