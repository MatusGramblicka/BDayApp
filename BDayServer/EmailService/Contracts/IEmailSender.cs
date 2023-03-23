using System.Threading.Tasks;
using EmailService.Contracts.Models;

namespace EmailService.Contracts
{
    public interface IEmailSender
    {
        void SendEmail(Message message);
        Task SendEmailAsync(Message message);
    }
}
