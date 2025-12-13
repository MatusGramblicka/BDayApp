using Infrastructure.Shared.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Core.IntegrationEvents.EventHandlers
{
    public class TravelOrderCreatedEventHandler : IEventHandler<TravelOrderCreatedEvent>
    {
        private readonly ILogger<TravelOrderCreatedEventHandler> _logger;
        public TravelOrderCreatedEventHandler(ILogger<TravelOrderCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(TravelOrderCreatedEvent @event)
        {
            _logger.LogInformation($"Received {nameof(TravelOrderCreatedEvent)} " +
                $"with id {@event.Id} " +
                $"and CreatedDate {@event.CreatedDate}");
            
            return Task.CompletedTask;
        }
    }
}
