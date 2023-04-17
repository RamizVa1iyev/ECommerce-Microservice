using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.RabbitMQ;
using RabbitMQ.Client;

namespace EventBus.Factory
{
    public class EventBusFactory
    {
        public static IEventBus Create(EventBusConfig config, IServiceProvider provider)
        {
            return config.EventBusType switch
            {
                EventBusType.AzureServiceBus => new EventBusServiceBus(config, provider),
                _ => new EventBusRabbitMQ(config, provider)
            };
        }
        public static IConnectionFactory CreateRabbitMQConnectionFactory(RabbitMQConfiguration configuration)
        {
            return new ConnectionFactory()
            {
                UserName= configuration.UserName,
                Password= configuration.Password,
                HostName= configuration.HostName,
                Port= configuration.Port,
                VirtualHost=configuration.VirtualHost
            };
        }
    }

    public class RabbitMQConfiguration
    {
        public string HostName { get; set; } = "localhost";

        public string VirtualHost { get; set; } = "/";

        public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;

        public string UserName { get; set; } = "guest";

        public string Password { get; set; } = "guest";
    }
}
