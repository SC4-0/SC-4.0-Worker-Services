using System;
using RabbitMQ.Client;

namespace RabbitMQHelper
{
    public interface IConnectionProvider: IDisposable
    {
        IConnection GetConnection();
    }
}
