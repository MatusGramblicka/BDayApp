using Entities.Models;
using Entities.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IPersonRepository
    {
        Task<PagedList<Person>> GetAllPersonsAsync(PersonParameters personParameters, bool trackChanges);
        Task<Person> GetPersonAsync(Guid personId, bool trackChanges);
        void CreatePerson(Person person);
        Task<IEnumerable<Person>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);
        void DeletePerson(Person person);
    }
}
