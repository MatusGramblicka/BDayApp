using Contracts.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;
using Repository.DatabaseAccessInterfaces;
using Repository.Extensions;
using Repository.Projections;
using Repository.Repositories;

namespace Repository;

// The context's query filter limits every query to the signed-in user's persons.
internal class PersonRepository(DbRepositoryContext repositoryContext)
    : RepositoryBase<Person>(repositoryContext), IPersonRepository
{
    public Task<PagedList<PersonDto>> GetAllPersonsAsync(PersonParameters personParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personParameters);

        return RepositoryContext.Persons
            .Select(PersonProjection.GetPersonSelected())
            .Search(personParameters.SearchTerm)
            .Sort(personParameters.OrderBy)
            .ToPagedListAsync(personParameters.PageNumber, personParameters.PageSize, cancellationToken);
    }

    public Task<PersonDto?> GetPersonAsync(Guid personId) =>
        RepositoryContext.Persons
            .Where(p => p.Id == personId)
            .Select(PersonProjection.GetPersonSelected())
            .SingleOrDefaultAsync();

    public Task<Person?> GetPersonAsync(Guid personId, bool trackChanges) =>
        FindByCondition(p => p.Id == personId, trackChanges)
            .SingleOrDefaultAsync();

    public void CreatePerson(Person person) => Create(person);

    public void DeletePerson(Person person) => Delete(person);
}
