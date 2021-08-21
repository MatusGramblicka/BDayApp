using BDayClient.Features;
using Entities.DataTransferObjects;
using Entities.Models;
using Entities.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BDayClient.HttpRepository
{
    public interface IPersonHttpRepository
    {
        Task<PagingResponse<Person>> GetPersons(PersonParameters personParameters);
        Task<Person> GetPerson(Guid id);
        Task CreatePerson(PersonForCreationDto person);
        Task<string> UploadPersonImage(MultipartFormDataContent content);
        Task UpdatePerson(Guid id, PersonForUpdateDto person);
        Task DeletePerson(Guid id);
    }
}
