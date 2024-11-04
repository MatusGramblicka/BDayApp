using Contracts;
using Entities;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;

namespace Repository;

public class EventRepository : RepositoryBase<Event>, IEventRepository
{
    public EventRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }

    public async Task<PagedList<Event>> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges)
    {
        var events = await FindAll(trackChanges)
            .Search(eventParameters.SearchTerm)
            .Sort(eventParameters.OrderBy)
            .ToListAsync();

        return PagedList<Event>
            .ToPagedList(events, eventParameters.PageNumber, eventParameters.PageSize);
    }

    public async Task<Event> GetEventAsync(Guid eventId, bool trackChanges) =>
        await FindByCondition(c => c.Id.Equals(eventId), trackChanges)
            .SingleOrDefaultAsync();

    public async Task<IEnumerable<Event>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges) =>
        await FindByCondition(x => ids.Contains(x.Id), trackChanges)
            .ToListAsync();

    public void CreateEvent(Event eventParam) => Create(eventParam);

    public void DeleteEvent(Event eventParam)
    {
        Delete(eventParam);
    }
}