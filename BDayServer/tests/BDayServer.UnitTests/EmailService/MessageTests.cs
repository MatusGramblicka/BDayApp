using EmailService.EmailServiceContracts;

namespace BDayServer.UnitTests.EmailService;

public class MessageTests
{
    [Fact]
    public void Constructor_CreatesMailboxPerRecipient()
    {
        var message = new Message(["a@test.com", "b@test.com"], "Subject", "Body", null);

        message.To.Select(m => m.Address).Should().Equal("a@test.com", "b@test.com");
        message.Subject.Should().Be("Subject");
        message.Content.Should().Be("Body");
        message.Attachments.Should().BeNull();
    }
}
