using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleRabbit
{
    public sealed class RabbitMQServiceProvider : IRabbitMQServiceProvider
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _factory;

        private IConnection _connection;
        private IModel _channel;

        private readonly List<RabbitInfo> _rabbitInfoCollection;
        private readonly List<Func<string, IDictionary<string, object>, bool>>  _callbackCollection;

        public RabbitMQServiceProvider(ILogger<RabbitMQServiceProvider> logger)
        {
            _logger = logger;
            _rabbitInfoCollection = new List<RabbitInfo>();
            _callbackCollection = new List<Func<string, IDictionary<string, object>, bool>>();

            this._factory = new ConnectionFactory
            {
                ClientProvidedName = Environment.GetEnvironmentVariable("RABBITMQ_CLIENT"),
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT")),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD"),
                VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST"),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };
            this._factory.AutomaticRecoveryEnabled = true;
            this._factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

            Reconnect();
        }

        private void Connect()
        {
            this._connection = this._factory.CreateConnection();
            this._connection.ConnectionShutdown += Connection_ConnectionShutdown;
            this._channel = this._connection.CreateModel();
        }

        private void Reconnect()
        {
            Cleanup();
            var mres = new ManualResetEventSlim(false);

            while (!mres.Wait(3000))
            {
                try
                {
                    Connect();
                    _logger.LogInformation("RabbitMQ Connected!");
                    mres.Set();
                   if(_rabbitInfoCollection != null)
                   {
                        for (int i = 0; i < _rabbitInfoCollection.Count; i++)
                        {
                            this.StartSubscribe(_rabbitInfoCollection[i], _callbackCollection[i]);
                        }
                   }
                  
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RabbitMQ Connect failed! {ex.Message}");
                }
            }
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogError("Connection broke!");
            Reconnect();
        }

        private void Cleanup()
        {
            try
            {
                if (this._channel != null)// && this._channel.IsOpen)
                {
                    this._channel.Close();
                    this._channel = null;
                }

                if (this._connection != null) //&& this._connection.IsOpen)
                {
                    this._connection.Close();
                    this._connection = null;
                }
            }
            catch (IOException ex)
            {
                _logger.LogError($"IOException! {ex.Message}");

            }
        }

        public void Subscribe(RabbitInfo rabbitInfo, Func<string, IDictionary<string, object>, bool> callback)
        {
            this._rabbitInfoCollection.Add(rabbitInfo);
            this._callbackCollection.Add(callback);

            this.StartSubscribe(rabbitInfo, callback);
        }

        private void StartSubscribe(RabbitInfo rabbitInfo, Func<string, IDictionary<string, object>, bool> callback)
        {
            _channel.QueueDeclare(rabbitInfo.queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(_channel);
            var ttl = new Dictionary<string, object>
            {
                {"x-message-ttl", 30000 }
            };

            if (string.IsNullOrEmpty(rabbitInfo.exchange) == false)
            {
                _channel.ExchangeDeclare(rabbitInfo.exchange, rabbitInfo.exchange_type, arguments: ttl);
                _channel.QueueBind(rabbitInfo.queue, rabbitInfo.exchange, rabbitInfo.routing_key);
            }

            consumer.Received += (sender, e) =>
            {
                var body = e.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                bool success = callback.Invoke(message, e.BasicProperties.Headers);
                if (success)
                {
                    this._channel.BasicAck(e.DeliveryTag, true);
                }
            };

            _channel.BasicConsume(rabbitInfo.queue, false, consumer);
        }

        public void Publish(string message, string queue, string routingKey, IDictionary<string, object> messageAttributes, string exchange = "")
        {
            var body = Encoding.UTF8.GetBytes(message);
            var properties = this._channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Headers = messageAttributes;
          
            this._channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            this._channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: properties, body: body);
        }


    }
}
