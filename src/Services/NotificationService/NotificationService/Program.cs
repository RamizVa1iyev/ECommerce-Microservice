using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.IndegrationEvents.EventHandlers;
using NotificationService.IndegrationEvents.Events;

internal class Program
{
    private static void Main(string[] args)
    {
        ServiceCollection services = new ServiceCollection();
        ConfigureServices(services);

        var sp = services.BuildServiceProvider();
        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Subscribe<OrderPaymentSuccessIntegrationEvent, OrderPaymentSuccessIntegrationHandler>();
        eventBus.Subscribe<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationHandler>();



        Console.WriteLine("Application is running");

        Console.ReadLine();
    }
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole());

        services.AddTransient<OrderPaymentSuccessIntegrationHandler>();
        services.AddTransient<OrderPaymentFailedIntegrationHandler>();

        services.AddSingleton<IEventBus>(sp =>
        {
            EventBusConfig config = new()
            {
                ConnectionRetryCount = 5,
                EventNameSuffix = "IntegrationEvent",
                SubscriberClientAppName = "NotificationService",
                EventBusType = EventBusType.RabbitMQ,
                Connection = EventBusFactory.CreateRabbitMQConnectionFactory(new RabbitMQConfiguration()
                {
                    HostName = "164.92.251.134"
                })
            };
            return EventBusFactory.Create(config, sp);
        });
    }
}