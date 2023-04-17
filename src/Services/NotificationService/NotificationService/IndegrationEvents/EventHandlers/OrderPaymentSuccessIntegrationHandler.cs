using EventBus.Base.Abstraction;
using Microsoft.Extensions.Logging;
using NotificationService.IndegrationEvents.Events;

namespace NotificationService.IndegrationEvents.EventHandlers
{
    public class OrderPaymentSuccessIntegrationHandler : IIntegrationEventHandler<OrderPaymentSuccessIntegrationEvent>
    {
        private readonly ILogger<OrderPaymentSuccessIntegrationHandler> _logger;
        public OrderPaymentSuccessIntegrationHandler(ILogger<OrderPaymentSuccessIntegrationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(OrderPaymentSuccessIntegrationEvent @event)
        {
            _logger.LogInformation($"Order Payment success with OrderId: {@event.OrderId}");
            return Task.CompletedTask;
        }
    }
}
