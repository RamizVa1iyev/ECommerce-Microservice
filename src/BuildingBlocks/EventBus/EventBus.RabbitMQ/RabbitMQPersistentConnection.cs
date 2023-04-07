using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EventBus.RabbitMQ
{
    public class RabbitMQPersistentConnection : IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection _connection;
        private object lock_object = new object();
        private bool _disposed;

        public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
        {
            this._connectionFactory = connectionFactory;
            this._retryCount = retryCount;
        }

        public bool ISConnected => this._connection != null && this._connection.IsOpen;

        public IModel CreateModel()
        {
            return this._connection.CreateModel();
        }

        public void Dispose()
        {
            this._disposed= true;
            this._connection.Dispose();
        }

        public bool TryConnect()
        {
            lock (lock_object)
            {
                var policy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>().WaitAndRetry(this._retryCount, retryAttemp => TimeSpan.FromSeconds(Math.Pow(2, retryAttemp)), (ex, time) =>
                {

                });

                policy.Execute(() =>
                {
                    this._connection = this._connectionFactory.CreateConnection();
                });

                if (this.ISConnected)
                {
                    this._connection.ConnectionShutdown += this.Connection_ConnectionShutdown;
                    this._connection.CallbackException += this.Connection_CallbackException;
                    this._connection.ConnectionBlocked += this.Connection_ConnectionBlocked;

                    return true;
                }

                return false;
            }
        }

        private void Connection_ConnectionBlocked(object? sender, global::RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
        {
            if(this._disposed) return;

            this.TryConnect();
        }

        private void Connection_CallbackException(object? sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (this._disposed) return;

            this.TryConnect();
        }

        private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            if (this._disposed) return;

            this.TryConnect();
        }
    }
}
