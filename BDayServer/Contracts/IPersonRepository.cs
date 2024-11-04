﻿using Entities.Models;
using Entities.RequestFeatures;

namespace Contracts;

public interface IPersonRepository
{
    Task<PagedList<Person>> GetAllPersonsAsync(PersonParameters personParameters, bool trackChanges);

    Task<Person> GetPersonAsync(Guid personId, bool trackChanges);

    void CreatePerson(Person person);

    Task<IEnumerable<Person>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);

    void DeletePerson(Person person);
}