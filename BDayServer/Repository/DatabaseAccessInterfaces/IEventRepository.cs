using Entities.Models;
using Entities.RequestFeatures;

namespace Repository.DatabaseAccessInterfaces;

public interface IEventRepository
{
    PagedList<Event> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges);

    Task<Event> GetEventAsync(Guid eventId, bool trackChanges);

    void CreateEvent(Event @event);

    Task<IEnumerable<Event>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);

    void DeleteEvent(Event @event);
}