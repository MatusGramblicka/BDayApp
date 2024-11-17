using Entities.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;

namespace Interfaces.DatabaseAccess;

public interface IPersonRepository
{
    PagedList<PersonDto> GetAllPersonsAsync(PersonParameters personParameters, bool trackChanges);

    Task<PersonDto?> GetPersonAsync(Guid personId);

    Task<Person?> GetPersonAsync(Guid personId, bool trackChanges);

    void CreatePerson(Person person);

    Task<IEnumerable<Person>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);

    void DeletePerson(Person person);
}