using System.Collections.Generic;
using System.Threading.Tasks;
using EmailService.Contracts.Models;
using Entities.DataTransferObjects.Person;

namespace EmailService.Contracts
{
    public interface IEmailPreparator
    {
        Task<List<Message>> PrepareMessage(List<PersonEmailDto> personsDto);
    }
}