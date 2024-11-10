using AutoMapper;
using Contracts.DatabaseAccess;
using Contracts.Exceptions;
using Contracts.Managers;
using Entities;
using Entities.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using System;

namespace Core;

public class PersonManager : IPersonManager
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    private readonly string _userName;

    public PersonManager(IRepositoryManager repository, IMapper mapper, UserManager<User> userManager,
        IGetUserProvider userData)
    {
        _repository = repository;
        _mapper = mapper;
        _userManager = userManager;

        _userName = userData.UserName;
    }

    public PagedList<PersonDto> GetPersons(PersonParameters personParameters)
    {
        ArgumentNullException.ThrowIfNull(personParameters, nameof(personParameters));

        return _repository.Person.GetAllPersonsAsync(personParameters, trackChanges: false);
    }

    public async Task<PersonDto?> GetPersonAsync(Guid personId)
    {
        return await _repository.Person.GetPersonAsync(personId);
    }

    public async Task<PersonDto> CreatePersonAsync(PersonForCreationDto personForCreationDto)
    {
        ArgumentNullException.ThrowIfNull(personForCreationDto, nameof(personForCreationDto));

        ArgumentNullException.ThrowIfNull(_userName, nameof(_userName));
        var user = await _userManager.FindByNameAsync(_userName);

        if (user is null)
            throw new UserNotExistException("User does not exist");

        var personEntity = _mapper.Map<Person>(personForCreationDto);
        personEntity.UserId = user.Id;

        _repository.Person.CreatePerson(personEntity);
        await _repository.SaveAsync();

        return _mapper.Map<PersonDto>(personEntity);
    }

    public async Task UpdatePersonAsync(Guid personId, PersonForUpdateDto personForUpdateDto)
    {
        ArgumentNullException.ThrowIfNull(personForUpdateDto, nameof(personForUpdateDto));

        var personFromDb = await _repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("User does not exist");

        _mapper.Map(personForUpdateDto, personFromDb);
        await _repository.SaveAsync();
    }

    public async Task DeletePersonAsync(Guid personId)
    {
        var personFromDb = await _repository.Person.GetPersonAsync(personId, true);

        if (personFromDb is null)
            throw new PersonNotExistException("User does not exist");

        _repository.Person.DeletePerson(personFromDb);
        await _repository.SaveAsync();
    }
}