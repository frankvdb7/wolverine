using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JasperFx.Core;
using Wolverine.Nats.Internal;
using Wolverine.Nats.Tests.Helpers;
using Wolverine.Tracking;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using NATS.Client.Core;

namespace Wolverine.Nats.Tests;

[Collection("NATS Integration Tests")]
public class DefaultIncomingMessageTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IHost? _receiver;
    private readonly string _receiverSubject = "default.incoming.test";
    private readonly NatsContainerFixture _fixture;

    public DefaultIncomingMessageTests(ITestOutputHelper output, NatsContainerFixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        _receiver = await Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.AddXunitLogging(_output))
            .UseWolverine(opts =>
            {
                opts.ServiceName = "Receiver";
                opts.UseNats(_fixture.ConnectionString).AutoProvision();
                opts.ListenToNatsSubject(_receiverSubject)
                    .DefaultIncomingMessage<DefaultTestMessage>()
                    .BufferedInMemory();
            })
            .StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_receiver != null)
        {
            await _receiver.StopAsync();
            _receiver.Dispose();
        }
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task receive_message_without_type_header_using_default_incoming_message()
    {
        var natsUrl = _fixture.ConnectionString;
        await using var nats = new NatsConnection(new NatsOpts { Url = natsUrl });
        await nats.ConnectAsync();

        var messageData = System.Text.Encoding.UTF8.GetBytes("{\"Text\":\"Hello without header\"}");

        // Send raw NATS message without Wolverine headers
        var tracked = await _receiver.TrackActivity()
            .Timeout(10.Seconds())
            .WaitForMessageToBeReceivedAt<DefaultTestMessage>(_receiver!)
            .ExecuteAndWaitAsync(c =>
            {
                return nats.PublishAsync(_receiverSubject, messageData).AsTask();
            });

        tracked.Received.SingleMessage<DefaultTestMessage>().Text.Should().Be("Hello without header");
    }
}

public record DefaultTestMessage(string Text);

public class DefaultTestMessageHandler
{
    public void Handle(DefaultTestMessage message)
    {
    }
}
