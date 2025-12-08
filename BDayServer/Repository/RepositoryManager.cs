using Interfaces.DatabaseAccess;

namespace Repository;

public class RepositoryManager(DbRepositoryContext repositoryContext) : IRepositoryManager
{
    private IPersonRepository _personRepository;
    private IEventRepository _eventRepository;

    public IPersonRepository Person => _personRepository ??= new PersonRepository(repositoryContext);

    public IEventRepository Event => _eventRepository ??= new EventRepository(repositoryContext);

    public Task SaveAsync() => repositoryContext.SaveChangesAsync();
}