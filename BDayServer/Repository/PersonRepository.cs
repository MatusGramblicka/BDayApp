using System;
using System.Collections.Generic;
using System.Linq;
using Entities.Models;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Entities;
using Microsoft.EntityFrameworkCore;
using Entities.RequestFeatures;
using Repository.Extensions;

namespace Repository
{
    class PersonRepository : RepositoryBase<Person>, IPersonRepository
    {
        public PersonRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<PagedList<Person>> GetAllPersonsAsync(PersonParameters personParameters, bool trackChanges)
        {
            var persons = await FindAll(trackChanges)
                //.FilterEmployees(employeeParameters.MinAge, employeeParameters.MaxAge)
                .Search(personParameters.SearchTerm)
                .Sort(personParameters.OrderBy)
                .ToListAsync();

            return PagedList<Person>
               .ToPagedList(persons, personParameters.PageNumber, personParameters.PageSize);
        }

        public async Task<Person> GetPersonAsync(Guid personId, bool trackChanges) =>
            await FindByCondition(c => c.Id.Equals(personId), trackChanges)
            .SingleOrDefaultAsync();

        public async Task<IEnumerable<Person>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges) =>
            await FindByCondition(x => ids.Contains(x.Id), trackChanges)
            .ToListAsync();

        public void CreatePerson(Person person) => Create(person);

        public void DeletePerson(Person person)
        {
            Delete(person);
        }
    }
}
