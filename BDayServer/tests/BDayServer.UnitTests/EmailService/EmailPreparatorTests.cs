using System.Globalization;
using EmailService.EmailService;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Repository.Repositories;

namespace BDayServer.UnitTests.EmailService;

public class EmailPreparatorTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    // 100 days after Today: never 0, 1 or 14 days away.
    private static readonly DateOnly FarAway = Today.AddDays(100);

    private readonly PostgreDbRepositoryContext _context = new(
        new DbContextOptionsBuilder<PostgreDbRepositoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public void Dispose() => _context.Dispose();

    private EmailPreparator CreateSut() => new(NullLogger<EmailPreparator>.Instance, _context);

    private User AddUser(string? email)
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
    public async Task PrepareMessages_NoPersons_ReturnsEmpty()
    {
        (await CreateSut().PrepareMessagesAsync(Today)).Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareMessages_NobodyCelebratesSoon_ReturnsEmpty()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Far", FarAway.AddYears(-30), FarAway);
        await _context.SaveChangesAsync();

        (await CreateSut().PrepareMessagesAsync(Today)).Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareMessages_BirthdayToday_SendsMessageToCreator()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Anna", Today.AddYears(-30), FarAway);
        await _context.SaveChangesAsync();

        var messages = await CreateSut().PrepareMessagesAsync(Today);

        var message = messages.Should().ContainSingle().Subject;
        message.To.Should().ContainSingle().Which.Address.Should().Be("owner@test.com");
        message.Subject.Should().Be("Celebration");
        message.Content.Should().Contain("Anna Test 22/11");

        var birthdaySection = message.Content.Split("Nameday:")[0];
        birthdaySection.Should().Contain("Anna");
    }

    [Fact]
    public async Task PrepareMessages_NameDayToday_IsListedUnderNameday()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Boris", birth: null, nameDay: Today.AddYears(-20));
        await _context.SaveChangesAsync();

        var message = (await CreateSut().PrepareMessagesAsync(Today)).Should().ContainSingle().Subject;

        var parts = message.Content.Split("Nameday:");
        parts[0].Should().NotContain("Boris");
        parts[1].Should().Contain("Boris");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(14, true)]
    [InlineData(2, false)]
    [InlineData(13, false)]
    [InlineData(15, false)]
    public async Task PrepareMessages_AnnouncesCelebrationsOnTheDayTheDayBeforeAnd14DaysAhead(int daysAhead,
        bool announced)
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Cyril", Today.AddDays(daysAhead).AddYears(-40), FarAway);
        await _context.SaveChangesAsync();

        (await CreateSut().PrepareMessagesAsync(Today)).Should().HaveCount(announced ? 1 : 0);
    }

    [Fact]
    public async Task PrepareMessages_GroupsMessagesPerCreator()
    {
        var first = AddUser("first@test.com");
        var second = AddUser("second@test.com");
        AddPerson(first, "Anna", Today.AddYears(-30), FarAway);
        AddPerson(first, "Cyril", Today.AddYears(-40), FarAway);
        AddPerson(second, "Dana", Today.AddYears(-25), FarAway);
        await _context.SaveChangesAsync();

        var messages = await CreateSut().PrepareMessagesAsync(Today);

        messages.Should().HaveCount(2);
        var forFirst = messages.Single(m => m.To.Single().Address == "first@test.com");
        forFirst.Content.Should().Contain("Anna").And.Contain("Cyril").And.NotContain("Dana");
        var forSecond = messages.Single(m => m.To.Single().Address == "second@test.com");
        forSecond.Content.Should().Contain("Dana").And.NotContain("Anna");
    }

    [Fact]
    public async Task PrepareMessages_AtTheEndOfTheYear_AnnouncesJanuaryCelebrations()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Tomorrow", new DateOnly(1990, 1, 1), FarAway);
        AddPerson(owner, "Fortnight", birth: null, nameDay: new DateOnly(2000, 1, 14));
        AddPerson(owner, "Later", new DateOnly(1990, 1, 2), FarAway);
        await _context.SaveChangesAsync();

        var message = (await CreateSut().PrepareMessagesAsync(new DateOnly(2026, 12, 31)))
            .Should().ContainSingle().Subject;

        var sections = message.Content.Split("Nameday:");
        sections[0].Should().Contain("Tomorrow Test 01/01").And.NotContain("Later");
        sections[1].Should().Contain("Fortnight Test 14/01");
    }

    [Theory]
    [InlineData("2027-02-28", true)] // not a leap year: celebrated on 28 February
    [InlineData("2027-03-01", false)]
    [InlineData("2028-02-28", true)] // leap year: the day before
    [InlineData("2028-02-29", true)]
    public async Task PrepareMessages_LeapDayBirthday(string today, bool announced)
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Leap", new DateOnly(1992, 2, 29), nameDay: new DateOnly(2000, 8, 15));
        await _context.SaveChangesAsync();

        var messages = await CreateSut().PrepareMessagesAsync(DateOnly.Parse(today, CultureInfo.InvariantCulture));

        if (announced)
            messages.Should().ContainSingle().Which.Content.Should().Contain("Leap Test 29/02");
        else
            messages.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("owner-without-domain")]
    [InlineData("not an address")]
    public async Task PrepareMessages_CreatorWithoutValidEmail_IsSkippedAndOthersStillGetTheirMessage(string? email)
    {
        var broken = AddUser(email);
        var valid = AddUser("valid@test.com");
        AddPerson(broken, "Anna", Today.AddYears(-30), FarAway);
        AddPerson(valid, "Boris", Today.AddYears(-20), FarAway);
        await _context.SaveChangesAsync();

        var messages = await CreateSut().PrepareMessagesAsync(Today);

        var message = messages.Should().ContainSingle().Subject;
        message.To.Single().Address.Should().Be("valid@test.com");
        message.Content.Should().Contain("Boris").And.NotContain("Anna");
    }

    [Fact]
    public async Task PrepareMessages_FormatsDatesAsDayMonthWithASlash_WhateverTheCurrentCulture()
    {
        var owner = AddUser("owner@test.com");
        AddPerson(owner, "Anna", Today.AddYears(-30), FarAway);
        await _context.SaveChangesAsync();

        var dottedCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        dottedCulture.DateTimeFormat.DateSeparator = ".";
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = dottedCulture;
        try
        {
            var message = (await CreateSut().PrepareMessagesAsync(Today)).Should().ContainSingle().Subject;

            message.Content.Should().Contain("Anna Test 22/11");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
