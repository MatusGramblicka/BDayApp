using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;

namespace Core.Managers.ManagerInterfaces;

public interface IPersonManager
{
    Task<PagedList<PersonDto>> GetPersonsAsync(PersonParameters personParameters,
        CancellationToken cancellationToken = default);

    Task<PersonDto?> GetPersonAsync(Guid personId);

    Task<PersonDto> CreatePersonAsync(PersonForCreationDto personForCreationDto);

    Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto);

    Task DeletePersonAsync(Guid personId);
}
