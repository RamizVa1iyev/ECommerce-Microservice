
namespace EventBus.Base
{
    public class SubscriptionInfo
    {
        public Type HandlerType { get; set; }

        public SubscriptionInfo(Type handlerType)
        {
            HandlerType = handlerType;
        }

        public static SubscriptionInfo Typed(Type handlerType)
        {
            return new SubscriptionInfo(handlerType);
        }
    }
}
