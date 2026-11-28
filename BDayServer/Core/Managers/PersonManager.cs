using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Mapping;
using Core.Services.Interfaces;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Repository.DatabaseAccessInterfaces;
using Repository.Services;

namespace Core.Managers;

// Azure SQL is the source of truth; every change is then copied to PostgreSQL for the birthday e-mails.
public class PersonManager(
    IRepositoryManager repository,
    UserManager<User> userManager,
    IGetUserProvider userData,
    IPersonMirror personMirror)
    : IPersonManager
{
    public Task<PagedList<PersonDto>> GetPersonsAsync(PersonParameters personParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personParameters);

        return repository.Person.GetAllPersonsAsync(personParameters, cancellationToken);
    }

    public Task<PersonDto?> GetPersonAsync(Guid personId) => repository.Person.GetPersonAsync(personId);

    public async Task<PersonDto> CreatePersonAsync(PersonForCreationDto personForCreationDto)
    {
        ArgumentNullException.ThrowIfNull(personForCreationDto);

        var user = userData.UserName is null ? null : await userManager.FindByNameAsync(userData.UserName);
        if (user is null)
            throw new UserNotExistException("User does not exist");

        var personEntity = personForCreationDto.ToEntity();
        personEntity.UserId = user.Id;
        repository.Person.CreatePerson(personEntity);
        await repository.SaveAsync();

        await personMirror.UpsertPersonAsync(personEntity);

        return personEntity.ToDto();
    }

    public async Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(personForUpdateDto);

        var personFromDb = await repository.Person.GetPersonAsync(personId, trackChanges: true)
                           ?? throw new PersonNotExistException("Person does not exist");

        personForUpdateDto.ApplyTo(personFromDb);
        await repository.SaveAsync();

        await personMirror.UpsertPersonAsync(personFromDb);
    }

    public async Task DeletePersonAsync(Guid personId)
    {
        var personFromDb = await repository.Person.GetPersonAsync(personId, trackChanges: true)
                           ?? throw new PersonNotExistException("Person does not exist");

        repository.Person.DeletePerson(personFromDb);
        await repository.SaveAsync();

        await personMirror.DeletePersonAsync(personId);
    }
}
