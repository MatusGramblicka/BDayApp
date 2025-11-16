using Infrastructure.Shared.EventBus;

namespace Core.IntegrationEvents;

public record TravelOrderCreatedEvent(int TravelOrderId) : Event;