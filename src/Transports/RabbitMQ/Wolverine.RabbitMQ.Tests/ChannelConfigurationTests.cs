using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;
using Wolverine.Runtime;
using Xunit;

namespace Wolverine.RabbitMQ.Tests;

public class ChannelConfigurationTests
{
    private IWolverineRuntime theRuntime;

    public ChannelConfigurationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());
        theRuntime = new WolverineRuntime(new WolverineOptions(), new ServiceProvider(services));
    }

    [Fact]
    public async Task can_customize_channel_creation()
    {
        var transport = new RabbitMqTransport();
        var expression = new RabbitMqTransportExpression(transport, new WolverineOptions());
        expression.ConfigureChannelCreation(o =>
        {
            o.PublisherConfirmationsEnabled = true;
        });

        await transport.ConnectAsync(theRuntime);

        await using var channel = await transport.ListeningConnection.CreateChannelAsync();

        channel.NextPublishSeqNo.ShouldBeGreaterThan(0); // This is an indirect way to check if publisher confirms are on
    }

    [Fact]
    public async Task can_customize_channel_creation_additively()
    {
        var transport = new RabbitMqTransport();
        var expression = new RabbitMqTransportExpression(transport, new WolverineOptions());
        expression.ConfigureChannelCreation(o =>
        {
            o.PublisherConfirmationsEnabled = true;
        })
        .ConfigureChannelCreation(o =>
        {
            o.ConsumerDispatchConcurrency = 2;
        });

        await transport.ConnectAsync(theRuntime);

        await using var channel = await transport.ListeningConnection.CreateChannelAsync();

        channel.NextPublishSeqNo.ShouldBeGreaterThan(0);
    }
}