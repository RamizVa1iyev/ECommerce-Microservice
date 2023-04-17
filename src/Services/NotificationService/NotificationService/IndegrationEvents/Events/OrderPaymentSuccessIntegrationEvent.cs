using EventBus.Base.Events;

namespace NotificationService.IndegrationEvents.Events
{
    public class OrderPaymentSuccessIntegrationEvent : IntegrationEvent
    {
        public int OrderId { get; set; }
        public OrderPaymentSuccessIntegrationEvent(int orderId) => OrderId = orderId;
    }
}
