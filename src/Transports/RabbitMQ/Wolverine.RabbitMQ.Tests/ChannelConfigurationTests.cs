using System;
using System.Threading.Tasks;
using Shouldly;
using Wolverine.RabbitMQ.Internal;
using Xunit;

namespace Wolverine.RabbitMQ.Tests;

public class ChannelConfigurationTests
{
    [Fact]
    public async Task can_customize_channel_creation()
    {
        var options = new WolverineOptions();
        options.UseRabbitMq()
            .ConfigureChannelCreation(o =>
            {
                o.PublisherConfirmationsEnabled = true;
            });

        await using var host = await WolverineHost.ForAsync(options);

        var transport = host.GetRabbitMqTransport();

        await using var channel = await transport.ListeningConnection.CreateChannelAsync();

        channel.NextPublishSeqNo.ShouldBeGreaterThan(0); // This is an indirect way to check if publisher confirms are on
    }

    [Fact]
    public async Task can_customize_channel_creation_additively()
    {
        var options = new WolverineOptions();
        options.UseRabbitMq()
            .ConfigureChannelCreation(o =>
            {
                o.PublisherConfirmationsEnabled = true;
            })
            .ConfigureChannelCreation(o =>
            {
                o.ConsumerDispatchConcurrency = 2;
            });

        await using var host = await WolverineHost.ForAsync(options);

        var transport = host.GetRabbitMqTransport();

        var wolverineOptions = new WolverineRabbitMqChannelOptions();
        transport.ChannelCreationOptions(wolverineOptions);

        wolverineOptions.PublisherConfirmationsEnabled.ShouldBeTrue();
        wolverineOptions.ConsumerDispatchConcurrency.ShouldBe((ushort)2);
    }
}