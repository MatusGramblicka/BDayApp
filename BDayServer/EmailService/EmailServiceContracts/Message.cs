﻿using Microsoft.AspNetCore.Http;
using MimeKit;

namespace Contracts.EmailService;

public class Message
{
    public List<MailboxAddress> To { get; set; } = new();

    public string Subject { get; set; }

    public string Content { get; set; }

    public IFormFileCollection? Attachments { get; set; }

    public Message(IEnumerable<string> to, string subject, string content, IFormFileCollection? attachments)
    {
        To.AddRange(to.Select(x => new MailboxAddress("Principal", x)));
        Subject = subject;
        Content = content;
        Attachments = attachments;
    }
}