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
    private static readonly DateOnly Today = new(2026, 11, 22);

    // 100 days after Today: never 0, 1 or 14 days away.
    private static readonly DateOnly FarAway = Today.AddDays(100);

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

    private async Task<IReadOnlyList<global::EmailService.EmailServiceContracts.Message>> PrepareMessagesAsync()
    {
        await using var context = fixture.PostgreSql.CreateContext();
        return await new EmailPreparator(NullLogger<EmailPreparator>.Instance, context).PrepareMessagesAsync(Today);
    }

    [Fact]
    public async Task PrepareMessage_SendsCelebrationsToTheCreator()
    {
        var owner = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(owner,
            ("Anna", Today.AddYears(-30), FarAway),
            ("Boris", null, Today.AddYears(-1)),
            ("Far", FarAway.AddYears(-20), FarAway));

        var message = (await PrepareMessagesAsync()).Should()
            .ContainSingle(m => m.To.Single().Address == owner.Email).Subject;

        message.Subject.Should().Be("Celebration");
        var sections = message.Content.Split("Nameday:");
        sections[0].Should().Contain("Anna Test 22/11").And.NotContain("Boris");
        sections[1].Should().Contain("Boris Test 22/11");
        message.Content.Should().NotContain("Far Test");
    }

    [Fact]
    public async Task PrepareMessage_PersonsWithoutCloseDays_AreNotReported()
    {
        var owner = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(owner, ("Far", FarAway.AddYears(-20), FarAway));

        (await PrepareMessagesAsync()).Should().NotContain(m => m.To.Single().Address == owner.Email);
    }

    [Fact]
    public async Task PrepareMessage_DifferentCreators_GetSeparateMessages()
    {
        var first = await fixture.CreatePostgreSqlUserAsync();
        var second = await fixture.CreatePostgreSqlUserAsync();
        await AddPersonsAsync(first, ("Cyril", Today.AddYears(-40), FarAway));
        await AddPersonsAsync(second, ("Dana", Today.AddYears(-25), FarAway));

        var messages = await PrepareMessagesAsync();

        messages.Single(m => m.To.Single().Address == first.Email).Content
            .Should().Contain("Cyril").And.NotContain("Dana");
        messages.Single(m => m.To.Single().Address == second.Email).Content
            .Should().Contain("Dana").And.NotContain("Cyril");
    }
}
