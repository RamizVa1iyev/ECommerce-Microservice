using EventBus.Base.Abstraction;
using EventBus.Base.Events;

namespace EventBus.Base.SubscriptionManagers
{
    public class InMemoryeventBusSubscriptionManager : IEventBusSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemove;

        public Func<string, string> eventNameGetter;


        public InMemoryeventBusSubscriptionManager(Func<string, string> eventNameGetter)
        {
            this._handlers = new Dictionary<string, List<SubscriptionInfo>>();
            this._eventTypes = new List<Type>();
            this.eventNameGetter = eventNameGetter;
        }

        public bool IsEmpty => !this._handlers.Keys.Any();

        public void Clear() => this._handlers.Clear();

        public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = this.GetEventKey<T>();

            this.AddSubscription(typeof(TH), eventName);

            if (!this._eventTypes.Contains(typeof(T)))
            {
                this._eventTypes.Add(typeof(T));
            }
        }

        private void AddSubscription(Type handlerType, string eventName)
        {
            if (!this.HasSubscriptionsForEvent(eventName))
            {
                this._handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (this._handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }

            this._handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var subscriptionToRemove = this.FindSubscriptionToRemove<T, TH>();
            var eventName = this.GetEventKey<T>();
            this.RemoveHandler(eventName,subscriptionToRemove);
        }

        private void RemoveHandler(string eventName, SubscriptionInfo subscriptionToRemove)
        {
            if (subscriptionToRemove != null)
            {
                this._handlers[eventName].Remove(subscriptionToRemove);

                if (!this._handlers[eventName].Any())
                {
                    this._handlers.Remove(eventName);
                    var eventType = this._eventTypes.FirstOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        this._eventTypes.Remove(eventType);
                    }

                    this.RaiseOnEventRemoved(eventName);
                }
            }
        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = this.OnEventRemove;
            handler?.Invoke(this, eventName);
        }

        private SubscriptionInfo FindSubscriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = this.GetEventKey<T>();
            return this.FindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo? FindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!this.HasSubscriptionsForEvent(eventName))
            {
                return null;
            }

            return this._handlers[eventName].SingleOrDefault(s=>s.HandlerType== handlerType);
        }

        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }

        public Type GetEventTypeByName(string eventName) => this._eventTypes.SingleOrDefault(t => t.Name == eventName);

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            string eventName = this.GetEventKey<T>();
            return this.GetHandlersForEvent(eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => this._handlers[eventName];

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            string eventName = this.GetEventKey<T>();

            return this.HasSubscriptionsForEvent(eventName);
        }

        public bool HasSubscriptionsForEvent(string eventName) => this._handlers.ContainsKey(eventName);
    }
}
