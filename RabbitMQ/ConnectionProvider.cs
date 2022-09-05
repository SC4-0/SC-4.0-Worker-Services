using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.IO;

namespace RabbitMQHelper
{
    public class ConnectionProvider : IConnectionProvider
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private bool _disposed;

        public ConnectionProvider(ILogger<ConnectionProvider> logger)
        {
            _logger = logger;

            this._factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT")),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD"),
                VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST"),
                RequestedHeartbeat = TimeSpan.FromSeconds(Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_HEARTBEAT"))),
                AutomaticRecoveryEnabled = true
            };

            Reconnect();
        }

        public IConnection GetConnection()
        {
            return _connection;
        }

        private void Connect()
        {
            Cleanup();

            this._connection = this._factory.CreateConnection();
            this._connection.ConnectionShutdown += Connection_ConnectionShutdown;
        }

        private void Reconnect()
        {

            var mres = new ManualResetEventSlim(false); // state is initially false

            while (!mres.Wait(3000)) // loop until state is true, checking every 3s
            {
                try
                {
                    Connect();
                    _logger.LogInformation("RabbitMQ Connected!");
                    mres.Set(); // state set to true - breaks out of loop
                }
                catch
                {
                    _logger.LogError("RabbitMQ Connection failed!");
                }
            }
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogError("RabbitMQ Connection broke!");

            Reconnect();
        }

        private void Cleanup()
        {
            try
            {

                if (this._connection != null)
                {
                    this._connection.Close();
                    this._connection = null;
                }
            }
            catch (IOException ex)
            {
                _logger.LogError("Exception: ", ex.Message);
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _connection?.Close();

            _disposed = true;
        }
    }
}
