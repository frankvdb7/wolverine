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
        var channelName = Guid.NewGuid().ToString();

        var options = new WolverineOptions();
        options.UseRabbitMq()
            .ConfigureChannelCreation(o =>
            {
                o.Name = channelName;
            });

        await using var host = await WolverineHost.ForAsync(options);

        var transport = host.GetRabbitMqTransport();

        await using var channel = await transport.ListeningConnection.CreateChannelAsync();

        channel.Name.ShouldBe(channelName);
    }
}