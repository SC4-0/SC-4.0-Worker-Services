using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SimpleRabbit;
using System;

namespace StateService.SignalR
{
    public class SignalRHub: Hub
    {
        private IRabbitMQServiceProvider _provider;
        private ILogger _logger;

        string exchange;
        string routingKey;
        string queue;

        public SignalRHub(IRabbitMQServiceProvider provider, ILogger<SignalRHub> logger)
        {
            _provider = provider;
            _logger = logger;

            exchange = Environment.GetEnvironmentVariable("RABBITMQ_STATE_EXCHANGE") ?? "";
            
            routingKey = Environment.GetEnvironmentVariable("RABBITMQ_STATE_ROUTING_KEY") ?? "";
        }

        public void ReceiveState(string user, string state)
        {
            _logger.LogInformation($"Received the string \'{state}\' from Frontend Service");
            _provider.Publish(state, queue, routingKey, null, exchange);
        }
    }
}
