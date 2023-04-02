using Contracts;
using Entities;
using System.Threading.Tasks;

namespace Repository
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly RepositoryContext _repositoryContext;
        private IPersonRepository _personRepository;
        private IEventRepository _eventRepository;

        public RepositoryManager(RepositoryContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
        }

        public IPersonRepository Person
        {
            get
            {
                if (_personRepository == null)
                    _personRepository = new PersonRepository(_repositoryContext);

                return _personRepository;
            }
        }

        public IEventRepository Event
        {
            get
            {
                if (_eventRepository == null)
                    _eventRepository = new EventRepository(_repositoryContext);

                return _eventRepository;
            }
        }

        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
