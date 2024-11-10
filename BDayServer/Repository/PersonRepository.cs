using Contracts.DatabaseAccess;
using Entities;
using Entities.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;
using Repository.Projections;

namespace Repository;

internal class PersonRepository(RepositoryContext repositoryContext)
    : RepositoryBase<Person>(repositoryContext), IPersonRepository
{
    public PagedList<PersonDto> GetAllPersonsAsync(PersonParameters personParameters, bool trackChanges)
    {
        ArgumentNullException.ThrowIfNull(personParameters, nameof(personParameters));

        var persons = RepositoryContext.Persons
            .Select(PersonProjection.GetPersonSelected())
            .Search(personParameters.SearchTerm)
            .Sort(personParameters.OrderBy);

        return PagedList<PersonDto>
            .ToPagedList(persons, personParameters.PageNumber, personParameters.PageSize);
    }

    public async Task<PersonDto?> GetPersonAsync(Guid personId)
    {
        return await RepositoryContext.Persons
            .Where(i => i.Id.Equals(personId))
            .Select(PersonProjection.GetPersonSelected())
            .SingleOrDefaultAsync();
    }

    public async Task<Person?> GetPersonAsync(Guid personId, bool trackChanges) =>
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