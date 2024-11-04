using Entities.Models;
using Entities.RequestFeatures;

namespace Contracts;

public interface IEventRepository
{
    Task<PagedList<Event>> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges);

    Task<Event> GetEventAsync(Guid eventId, bool trackChanges);

    void CreateEvent(Event Event);

    Task<IEnumerable<Event>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);

    void DeleteEvent(Event Event);
}