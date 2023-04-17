using EventBus.Base.Abstraction;
using Microsoft.Extensions.Logging;
using NotificationService.IndegrationEvents.Events;

namespace NotificationService.IndegrationEvents.EventHandlers
{
    public class OrderPaymentFailedIntegrationHandler : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
    {
        private readonly ILogger<OrderPaymentFailedIntegrationEvent> _logger;
        public OrderPaymentFailedIntegrationHandler(ILogger<OrderPaymentFailedIntegrationEvent> logger)
        {
            _logger = logger;
        }

        public Task Handle(OrderPaymentFailedIntegrationEvent @event)
        {
            //send fail email

            _logger.LogInformation($"Order Payment failed with OrderId: {@event.OrderId}, ErroMessage: {@event.ErrorMessage}");
            return Task.CompletedTask;
        }
    }
}
