using System;
using System.Collections.Generic;

namespace SimpleRabbit
{
    public interface IRabbitMQServiceProvider
    {
        void Publish(string message, string queue, string routingKey, IDictionary<string, object> messageAttributes, string exchange = "");
        void Subscribe(string queue, Func<string, IDictionary<string, object>, bool> callback);
    }
}