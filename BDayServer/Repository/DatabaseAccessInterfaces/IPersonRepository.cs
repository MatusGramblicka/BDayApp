using Contracts.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;

namespace Repository.DatabaseAccessInterfaces;

public interface IPersonRepository
{
    Task<PagedList<PersonDto>> GetAllPersonsAsync(PersonParameters personParameters,
        CancellationToken cancellationToken = default);

    Task<PersonDto?> GetPersonAsync(Guid personId);

    Task<Person?> GetPersonAsync(Guid personId, bool trackChanges);

    void CreatePerson(Person person);

    void DeletePerson(Person person);
}
