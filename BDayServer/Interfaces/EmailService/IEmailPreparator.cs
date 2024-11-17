using Contracts.EmailService.Models;

namespace Interfaces.EmailService;

public interface IEmailPreparator
{
    List<Message>? PrepareMessage();
}