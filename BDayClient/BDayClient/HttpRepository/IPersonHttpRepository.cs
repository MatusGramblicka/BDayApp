using BDayClient.Features;
using BDayClient.Http;
using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;

namespace BDayClient.HttpRepository;

/// <summary>Person calls. Every method throws an <see cref="ApiException"/> when the call fails.</summary>
public interface IPersonHttpRepository
{
    Task<PagingResponse<PersonDto>> GetPersons(PersonParameters personParameters,
        CancellationToken cancellationToken = default);

    /// <summary>All persons, page by page (the API returns at most 50 per page).</summary>
    Task<List<PersonDto>> GetAllPersons(CancellationToken cancellationToken = default);

    /// <summary>The person, or null when there is no person with this id (404).</summary>
    Task<PersonDto?> GetPerson(Guid id, CancellationToken cancellationToken = default);

    Task CreatePerson(PersonForCreationDto person);

    /// <summary>Uploads an image and returns the path the server stores it under ("StaticFiles/Images/name.png").</summary>
    Task<string> UploadPersonImage(MultipartFormDataContent content);

    Task UpdatePerson(Guid id, PersonForUpdateDto person);
    Task DeletePerson(Guid id);
}
