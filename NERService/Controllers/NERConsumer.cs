using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.SignalR;
using NERService.SignalR;
using SimpleRabbit;

namespace NERService.Controllers
{
    public class NERConsumer: BackgroundService
    {
        private IRabbitMQServiceProvider _provider;
        private IHubContext<SignalRHub> _hub;

        RabbitInfo queue;
        public NERConsumer(IRabbitMQServiceProvider provider, IHubContext<SignalRHub> hub)
        {
            _provider = provider;
            _hub = hub;

            queue = new RabbitInfo
            {
                queue = Environment.GetEnvironmentVariable("RABBITMQ_NER_QUEUE")
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _provider.Subscribe(queue, (string message, IDictionary<string, object> headers) => {
                Console.WriteLine($"Retrieved Message From RabbitMQ: {message}");
                _hub.Clients.All.SendAsync("NER", "NERService", message);
                return true;
            });

            return Task.CompletedTask;
        }
    }
}
