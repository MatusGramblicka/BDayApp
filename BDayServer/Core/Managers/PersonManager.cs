using AutoMapper;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services;
using Core.Services.Interfaces;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Repository;
using Repository.DatabaseAccessInterfaces;
using Repository.Repositories;
using Repository.Services;

namespace Core.Managers;

public class PersonManager(
    IRepositoryManager repository,
    IMapper mapper,
    UserManager<User> userManager,
    IGetUserProvider userData,
    PostgreDbRepositoryContext postgreDbContext)
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
            throw new UserNotExistException("Person does not exist");

        var personEntity = mapper.Map<Person>(personForCreationDto);
        personEntity.UserId = user.Id;
        repository.Person.CreatePerson(personEntity);
        await repository.SaveAsync();

        // for postgreSql
        var personEntityPostgreSql = mapper.Map<Person>(personForCreationDto);
        personEntityPostgreSql.UserId = user.Id;
        personEntityPostgreSql.Id = personEntity.Id;
        postgreDbContext.Add(personEntityPostgreSql);
        await postgreDbContext.SaveChangesAsync();

        return mapper.Map<PersonDto>(personEntity);
    }

    public async Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(personForUpdateDto, nameof(personForUpdateDto));

        var personFromDb = await repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("Person does not exist");

        mapper.Map(personForUpdateDto, personFromDb);
        await repository.SaveAsync();

        // for postgreSql
        var personFromPostgreDb = postgreDbContext.Persons
                                                  .Where(p => p.Id == personId)
                                                  .FirstOrDefault();

        if (personFromPostgreDb is null)
            throw new PersonNotExistException("Person does not exist in PostgreSql");

        mapper.Map(personForUpdateDto, personFromPostgreDb);
        await postgreDbContext.SaveChangesAsync();

    }

    public async Task DeletePersonAsync(Guid personId)
    {
        var personFromDb = await repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("Person does not exist");

        repository.Person.DeletePerson(personFromDb);
        await repository.SaveAsync();

        // for postgreSql
        var personFromPostgreDb = postgreDbContext.Persons
                                                  .Where(p => p.Id == personId)
                                                  .FirstOrDefault();
        
        if (personFromPostgreDb is null)
            throw new PersonNotExistException("Person does not exist in PostgreSql");

        postgreDbContext.Remove(personFromPostgreDb);
        await postgreDbContext.SaveChangesAsync();
    }
}