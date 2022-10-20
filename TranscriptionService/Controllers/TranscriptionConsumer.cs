using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.SignalR;
using TranscriptionService.SignalR;
using SimpleRabbit;

namespace TranscriptionService.Controllers
{
    public class TranscriptionConsumer : BackgroundService
    {
        private IRabbitMQServiceProvider _provider;
        private IHubContext<SignalRHub> _hub;

        RabbitInfo subscriptionInfo;
        public TranscriptionConsumer(IRabbitMQServiceProvider provider, IHubContext<SignalRHub> hub)
        {
            _provider = provider;
            _hub = hub;

            subscriptionInfo = new RabbitInfo
            {
                queue = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_QUEUE") ?? "",
                exchange = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_EXCHANGE") ?? "",
                routing_key = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_ROUTING_KEY") ?? ""
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _provider.Subscribe(subscriptionInfo, (string message, IDictionary<string, object> headers) =>
            {
                Console.WriteLine($"Retrieved Message From RabbitMQ: {message}");
                _hub.Clients.All.SendAsync("Transcription", "TranscriptionService", message);
                return true;
            });

            return Task.CompletedTask;
        }
    }
}
