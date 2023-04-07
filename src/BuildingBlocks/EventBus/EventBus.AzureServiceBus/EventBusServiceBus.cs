using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient _topicClient;
        private ManagementClient _managementClient;
        private readonly ILogger _logger;

        public EventBusServiceBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider) : base(eventBusConfig, serviceProvider)
        {
            this._managementClient = new ManagementClient(eventBusConfig.EventBusConnectionString);
            this._topicClient = this.CreateTopicClient();
            this._logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
        }

        private ITopicClient CreateTopicClient()
        {
            if (this._topicClient == null || this._topicClient.IsClosedOrClosing)
            {
                this._topicClient = new TopicClient(base.EventBusConfig.EventBusConnectionString, base.EventBusConfig.DefaultTopicName, RetryPolicy.Default);

            }

            if (!this._managementClient.TopicExistsAsync(base.EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
                this._managementClient.CreateTopicAsync(base.EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();

            return this._topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;
            eventName = base.ProcessEventName(eventName);

            var eventString = JsonConvert.SerializeObject(@event);
            var bodyArray = Encoding.UTF8.GetBytes(eventString);

            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArray,
                Label = eventName
            };

            this._topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            _ = base.ProcessEventName(eventName);

            if (!base.EventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptionClient = this.CreateSubscriptionClientIfNotExist(eventName);

                this.RegisterSubscriptionClientMessageHandler(subscriptionClient);
            }

            this._logger.LogInformation("Subscribing to event {EventName} with {EventHandler}",eventName,typeof(TH).Name);

            base.EventBusSubscriptionManager.AddSubscription<T, TH>();
        }

        public override void UnSubscribe<T, TH>()
        {
            var eventName = typeof(T).Name;

            try
            {
                var subscriptionClient = this.CreateSubscriptionClient(eventName);

                subscriptionClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                this._logger.LogWarning("The messaging entity {eventName} could not be found.", eventName);
            }

            this._logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

            base.EventBusSubscriptionManager.RemoveSubscription<T, TH>();
        }

        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
               async (message,token) =>
               {
                   var eventName = message.Label;
                   var messageData = Encoding.UTF8.GetString(message.Body);

                   if(await base.ProcessEvent(base.ProcessEventName(eventName), messageData))
                   {
                       await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                   }
               },
               new MessageHandlerOptions(ExceptionRecieveHandler) { AutoComplete=false,MaxConcurrentCalls=10});
        }

        private Task ExceptionRecieveHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            this._logger.LogError(ex,"ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}",ex.Message,context);

            return Task.CompletedTask;
        }

        private SubscriptionClient CreateSubscriptionClient(string eventnName)
        {
            return new SubscriptionClient(base.EventBusConfig.EventBusConnectionString, base.EventBusConfig.DefaultTopicName, base.GetSubscriptionName(eventnName));
        }

        private ISubscriptionClient CreateSubscriptionClientIfNotExist(string eventName)
        {
            var subscription = this.CreateSubscriptionClient(eventName);

            var exist = this._managementClient.SubscriptionExistsAsync(base.EventBusConfig.DefaultTopicName, base.GetSubscriptionName(eventName)).GetAwaiter().GetResult();

            if (!exist)
            {
                this._managementClient.CreateSubscriptionAsync(base.EventBusConfig.DefaultTopicName, base.GetSubscriptionName(eventName)).GetAwaiter().GetResult();
                this.RemoveDefaultRule(subscription);
            }

            this.CreateRuleIfNotExist(this.ProcessEventName(eventName), subscription);

            return subscription;
        }

        private void CreateRuleIfNotExist(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExist;

            try
            {
                var rule = this._managementClient.GetRuleAsync(base.EventBusConfig.DefaultTopicName, base.GetSubscriptionName(eventName), eventName).GetAwaiter().GetResult();
                ruleExist = rule != null;
            }
            catch (MessagingEntityNotFoundException)
            {
                ruleExist = false;
            }

            if (!ruleExist)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription()
                {
                    Name = eventName,
                    Filter = new CorrelationFilter() { Label = eventName }
                }).GetAwaiter().GetResult();
            }
        }

        private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient.RemoveRuleAsync(RuleDescription.DefaultRuleName).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                this._logger.LogWarning("The messaging entity {DefaultRuleName} could not be found.", RuleDescription.DefaultRuleName);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            this._topicClient.CloseAsync().GetAwaiter().GetResult();
            this._managementClient.CloseAsync().GetAwaiter().GetResult();

            this._topicClient = null;
            this._managementClient = null;
        }
    }
}
