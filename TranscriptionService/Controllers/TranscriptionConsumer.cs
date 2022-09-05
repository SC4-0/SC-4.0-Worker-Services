using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQHelper;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.SignalR;
using TranscriptionService.SignalR;

namespace TranscriptionService.Controllers
{
    public class TranscriptionConsumer : BackgroundService
    {
        private ISubscriber _subscriber;
        private IHubContext<SignalRHub> _hub;
        public TranscriptionConsumer(ISubscriber subscriber, IHubContext<SignalRHub> hub)
        {
            _subscriber = subscriber;
            _hub = hub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _subscriber.Subscribe((string message, IDictionary<string, object> headers) =>
            {
                Console.WriteLine($"Retrieved Message From RabbitMQ: {message}");
                _hub.Clients.All.SendAsync("Transcription", "TranscriptionService", message);
                return true;
            });

            return Task.CompletedTask;
        }
    }
}
