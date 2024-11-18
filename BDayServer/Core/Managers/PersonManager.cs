using AutoMapper;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Entities.Models;
using Entities.RequestFeatures;
using Interfaces;
using Interfaces.DatabaseAccess;
using Interfaces.Managers;
using Interfaces.UserProvider;
using Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class PersonManager(
    IRepositoryManager repository,
    IMapper mapper,
    UserManager<User> userManager,
    IGetUserProvider userData)
    : IPersonManager
{
    private readonly string _userName = userData.UserName;

    public PagedList<PersonDto> GetPersons(PersonParameters personParameters)
    {
        ArgumentNullException.ThrowIfNull(personParameters, nameof(personParameters));

        return repository.Person.GetAllPersonsAsync(personParameters, trackChanges: false);
    }

    public async Task<PersonDto?> GetPersonAsync(Guid personId)
    {
        return await repository.Person.GetPersonAsync(personId);
    }

    public async Task<PersonDto> CreatePersonAsync(PersonForCreationDto personForCreationDto)
    {
        ArgumentNullException.ThrowIfNull(personForCreationDto, nameof(personForCreationDto));

        ArgumentNullException.ThrowIfNull(_userName, nameof(_userName));
        var user = await userManager.FindByNameAsync(_userName);

        if (user is null)
            throw new UserNotExistException("User does not exist");

        var personEntity = mapper.Map<Person>(personForCreationDto);
        personEntity.UserId = user.Id;

        repository.Person.CreatePerson(personEntity);
        await repository.SaveAsync();

        return mapper.Map<PersonDto>(personEntity);
    }

    public async Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(personForUpdateDto, nameof(personForUpdateDto));

        var personFromDb = await repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("User does not exist");

        mapper.Map(personForUpdateDto, personFromDb);
        await repository.SaveAsync();
    }

    public async Task DeletePersonAsync(Guid personId)
    {
        var personFromDb = await repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("User does not exist");

        repository.Person.DeletePerson(personFromDb);
        await repository.SaveAsync();
    }
}