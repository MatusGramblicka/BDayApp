using EmailService.Contracts.Models;

namespace EmailService.Contracts;

public interface IEmailPreparator
{
    List<Message>? PrepareMessage();
}