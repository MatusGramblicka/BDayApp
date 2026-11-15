using EmailService.EmailService;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Repository.Repositories;

namespace BDayServer.UnitTests.EmailService;

public class EmailPreparatorTests : IDisposable
{
    private readonly PostgreDbRepositoryContext _context = new(
        new DbContextOptionsBuilder<PostgreDbRepositoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // A date far enough from today (in either direction, across year boundaries) to never be "close".
    private static DateOnly FarAway => Today.AddDays(100);

    public void Dispose() => _context.Dispose();

    private EmailPreparator CreateSut() => new(NullLogger<EmailPreparator>.Instance, _context);

    private User AddUser(string email)
    {
        var user = new User { Id = Guid.NewGuid().ToString(), Email = email, UserName = email, RefreshToken = "" };
        _context.Users.Add(user);
        return user;
    }

    private void AddPerson(User owner, string name, DateOnly? birth, DateOnly nameDay) =>
        _context.Persons.Add(new Person
        {
            Id = Guid.NewGuid(),
            Name = name,
            Surname = "Test",
            DayOfBirth = birth,
            DayOfNameDay = nameDay,
            UserId = owner.Id
        });

    [Fact]
    public void PrepareMessage_NoPersons_ReturnsNull()
    {
        CreateSut().PrepareMessage().Should().BeNull();
    }

    [Fact]
    public void PrepareMessage_NobodyCelebratesSoon_ReturnsNull()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Far", FarAway.AddYears(-30), FarAway);
        _context.SaveChanges();

        CreateSut().PrepareMessage().Should().BeNull();
    }

    [Fact]
    public void PrepareMessage_BirthdayToday_SendsMessageToCreator()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Anna", Today.AddYears(-30), FarAway);
        _context.SaveChanges();

        var messages = CreateSut().PrepareMessage();

        var message = messages.Should().ContainSingle().Subject;
        message.To.Should().ContainSingle().Which.Address.Should().Be("owner@test.com");
        message.Subject.Should().Be("Celebration");
        message.Content.Should().Contain($"Anna Test {Today:dd/MM}");

        var birthdaySection = message.Content.Split("Nameday:")[0];
        birthdaySection.Should().Contain("Anna");
    }

    [Fact]
    public void PrepareMessage_NameDayToday_IsListedUnderNameday()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Boris", birth: null, nameDay: Today.AddYears(-20));
        _context.SaveChanges();

        var message = CreateSut().PrepareMessage().Should().ContainSingle().Subject;

        var parts = message.Content.Split("Nameday:");
        parts[0].Should().NotContain("Boris");
        parts[1].Should().Contain("Boris");
    }

    [Fact]
    public void PrepareMessage_GroupsMessagesPerCreator()
    {
        var first = AddUser("first@test.com");
        var second = AddUser("second@test.com");
        AddPerson(first, "Anna", Today.AddYears(-30), FarAway);
        AddPerson(first, "Cyril", Today.AddYears(-40), FarAway);
        AddPerson(second, "Dana", Today.AddYears(-25), FarAway);
        _context.SaveChanges();

        var messages = CreateSut().PrepareMessage()!;

        messages.Should().HaveCount(2);
        var forFirst = messages.Single(m => m.To.Single().Address == "first@test.com");
        forFirst.Content.Should().Contain("Anna").And.Contain("Cyril").And.NotContain("Dana");
        var forSecond = messages.Single(m => m.To.Single().Address == "second@test.com");
        forSecond.Content.Should().Contain("Dana").And.NotContain("Anna");
    }
}
