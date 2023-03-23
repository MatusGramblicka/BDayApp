using System.Collections.Generic;
using System.Threading.Tasks;
using EmailService.Contracts.Models;

namespace EmailService.Contracts
{
    public interface IEmailPreparator
    {
        Task<List<Message>> PrepareMessage();
    }
}