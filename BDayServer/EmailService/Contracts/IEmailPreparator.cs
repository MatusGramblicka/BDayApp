using EmailService.Contracts.Models;
using System.Collections.Generic;

namespace EmailService.Contracts
{
    public interface IEmailPreparator
    {
        List<Message> PrepareMessage();
    }
}