using EmailService.Contracts.Models;
using MimeKit;

namespace EmailService.Extensions;

public static class EmailSenderExtensions
{
    public static MimeMessage CreateEmailMessage(this Message message, string from)
    {
        ArgumentNullException.ThrowIfNull(message);

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("Principal", from));
        emailMessage.To.AddRange(message.To);
        emailMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { TextBody = message.Content };

        if (message.Attachments is not null && message.Attachments.Any())
        {
            foreach (var attachment in message.Attachments)
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    attachment.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }

                bodyBuilder.Attachments.Add(attachment.FileName, fileBytes,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        emailMessage.Body = bodyBuilder.ToMessageBody();
        return emailMessage;
    }
}