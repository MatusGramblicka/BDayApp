using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;

namespace Core.Managers.ManagerInterfaces;

public interface IPersonManager
{
    PagedList<PersonDto> GetPersons(PersonParameters personParameters);

    Task<PersonDto?> GetPersonAsync(Guid personId);

    Task<PersonDto> CreatePersonAsync(PersonForCreationDto personForCreationDto);

    Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto);

    Task DeletePersonAsync(Guid personId);
}