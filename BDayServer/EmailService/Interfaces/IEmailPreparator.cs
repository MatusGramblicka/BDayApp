using EmailService.EmailServiceContracts;

namespace EmailService.Interfaces;

public interface IEmailPreparator
{
    List<Message>? PrepareMessage();
}