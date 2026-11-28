using Entities.Models;
using Entities.RequestFeatures;

namespace Repository.DatabaseAccessInterfaces;

public interface IEventRepository
{
    Task<PagedList<Event>> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<Event?> GetEventAsync(Guid eventId, bool trackChanges);

    void CreateEvent(Event @event);

    void DeleteEvent(Event @event);
}
