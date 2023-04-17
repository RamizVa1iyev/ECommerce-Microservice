using EventBus.Base.Abstraction;
using EventBus.Base.SubscriptionManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        public EventBusConfig EventBusConfig { get; set; }

        public readonly IServiceProvider ServiceProvider;

        public readonly IEventBusSubscriptionManager EventBusSubscriptionManager;

        protected BaseEventBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider)
        {
            this.EventBusConfig = eventBusConfig;
            ServiceProvider = serviceProvider;
            EventBusSubscriptionManager = new InMemoryeventBusSubscriptionManager(this.ProcessEventName);
        }

        public virtual string ProcessEventName(string eventName) 
        {
            if (this.EventBusConfig.DeleteEventPrefix)
                eventName = eventName.TrimStart(this.EventBusConfig.EventNamePrefix.ToArray());

            if (this.EventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(this.EventBusConfig.EventNameSuffix.ToArray());

            return eventName;
        }

        public virtual string GetSubscriptionName(string eventName)
        {
            return $"{this.EventBusConfig.SubscriberClientAppName}.{this.ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            this.EventBusConfig = null;
            this.EventBusSubscriptionManager.Clear();
        }

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = this.ProcessEventName(eventName);

            var processed = false;

            if (this.EventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = this.EventBusSubscriptionManager.GetHandlersForEvent(eventName);

                using var scope = this.ServiceProvider.CreateScope();

                foreach (var subscription in subscriptions)
                {
                    var handler = this.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                        continue;

                    var eventType = this.EventBusSubscriptionManager.GetEventTypeByName($"{this.EventBusConfig.EventNamePrefix}{eventName}{this.EventBusConfig.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);


                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }

            return processed;
        }

        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

        public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    }
}
