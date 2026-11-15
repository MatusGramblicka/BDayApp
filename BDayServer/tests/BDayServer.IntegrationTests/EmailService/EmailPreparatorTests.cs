using BDayServer.IntegrationTests.Fixtures;
using Entities.Models;
using Microsoft.Extensions.Logging.Abstractions;
using EmailPreparator = EmailService.EmailService.EmailPreparator;

namespace BDayServer.IntegrationTests.EmailService;

/// <summary>
/// Verifies the scheduled e-mail query (persons joined with their creator) against a real PostgreSQL database.
/// </summary>
[Trait("Category", "Docker")]
[Collection(DatabaseCollection.Name)]
public class EmailPreparatorTests(MigratedDatabasesFixture fixture)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // Far enough from today (also across a year boundary) to never be reported.
    private static DateOnly FarAway => Today.AddDays(100);

    private async Task AddPersonsAsync(User owner, params (string Name, DateOnly? Birth, DateOnly NameDay)[] persons)
    {
        await using var context = fixture.PostgreSql.CreateContext();
        foreach (var (name, birth, nameDay) in persons)
        {
            context.Persons.Add(new Person
            {
                Id = Guid.NewGuid(),
                Name = name,
                Surname = "Test",
                DayOfBirth = birth,
                DayOfNameDay = nameDay,
                UserId = owner.Id
            });
        }

        await context.SaveChangesAsync();
    }

    private List<global::EmailService.EmailServiceContracts.Message> PrepareMessages()
    {
        using var context = fixture.PostgreSql.CreateContext();
        return new EmailPreparator(NullLogger<EmailPreparator>.Instance, context).PrepareMessage() ?? [];
    }

    [Fact]
    public async Task PrepareMessage_SendsCelebrationsToTheCreator()
    {
        var owner = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(owner,
            ("Anna", Today.AddYears(-30), FarAway),
            ("Boris", null, Today.AddYears(-1)),
            ("Far", FarAway.AddYears(-20), FarAway));

        var message = PrepareMessages().Should()
            .ContainSingle(m => m.To.Single().Address == owner.Email).Subject;

        message.Subject.Should().Be("Celebration");
        var sections = message.Content.Split("Nameday:");
        sections[0].Should().Contain($"Anna Test {Today:dd/MM}").And.NotContain("Boris");
        sections[1].Should().Contain($"Boris Test {Today:dd/MM}");
        message.Content.Should().NotContain("Far Test");
    }

    [Fact]
    public async Task PrepareMessage_PersonsWithoutCloseDays_AreNotReported()
    {
        var owner = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(owner, ("Far", FarAway.AddYears(-20), FarAway));

        PrepareMessages().Should().NotContain(m => m.To.Single().Address == owner.Email);
    }

    [Fact]
    public async Task PrepareMessage_DifferentCreators_GetSeparateMessages()
    {
        var first = await fixture.CreatePostgreSqlUserAsync();
        var second = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(first, ("Cyril", Today.AddYears(-40), FarAway));
        await AddPersonsAsync(second, ("Dana", Today.AddYears(-25), FarAway));

        var messages = PrepareMessages();

        messages.Single(m => m.To.Single().Address == first.Email).Content
            .Should().Contain("Cyril").And.NotContain("Dana");
        messages.Single(m => m.To.Single().Address == second.Email).Content
            .Should().Contain("Dana").And.NotContain("Cyril");
    }
}
