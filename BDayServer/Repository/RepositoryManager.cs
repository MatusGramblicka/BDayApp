using Contracts.DatabaseAccess;
using Entities;

namespace Repository;

public class RepositoryManager : IRepositoryManager
{
    private readonly RepositoryContext _repositoryContext;

    private IPersonRepository _personRepository;
    private IEventRepository _eventRepository;

    public RepositoryManager(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
    }

    public IPersonRepository Person => _personRepository ??= new PersonRepository(_repositoryContext);

    public IEventRepository Event => _eventRepository ??= new EventRepository(_repositoryContext);

    public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
}