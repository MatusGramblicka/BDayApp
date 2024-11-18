using Contracts.EmailService;

namespace Interfaces.EmailService;

public interface IEmailPreparator
{
    List<Message>? PrepareMessage();
}