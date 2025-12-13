using Entities.Models;
using Entities.RequestFeatures;
using Interfaces.DatabaseAccess;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;

namespace Repository;

public class EventRepository : RepositoryBase<Event>, IEventRepository
{
    public EventRepository(DbRepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }

    public PagedList<Event> GetAllEventsAsync(EventParameters eventParameters, bool trackChanges)
    {
        var events = FindAll(trackChanges)
            .Search(eventParameters.SearchTerm)
            .Sort(eventParameters.OrderBy);

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