using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;
using Repository.DatabaseAccessInterfaces;
using Repository.Extensions;
using Repository.Repositories;

namespace Repository;

// The context's query filter limits every query to the signed-in user's events.
public class EventRepository(DbRepositoryContext repositoryContext)
    : RepositoryBase<Event>(repositoryContext), IEventRepository
{
    public Task<PagedList<Event>> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventParameters);

        return FindAll(trackChanges)
            .Search(eventParameters.SearchTerm)
            .Sort(eventParameters.OrderBy)
            .ToPagedListAsync(eventParameters.PageNumber, eventParameters.PageSize, cancellationToken);
    }

    public Task<Event?> GetEventAsync(Guid eventId, bool trackChanges) =>
        FindByCondition(e => e.Id == eventId, trackChanges)
            .SingleOrDefaultAsync();

    public void CreateEvent(Event @event) => Create(@event);

    public void DeleteEvent(Event @event) => Delete(@event);
}
