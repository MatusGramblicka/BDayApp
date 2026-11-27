using BDayServer.UnitTests.TestUtilities;
using Core.Services;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Repository.Repositories;

namespace BDayServer.UnitTests.Core;

public sealed class PostgresPersonMirrorTests : IDisposable
{
    private readonly PostgreDbRepositoryContext _mirror = new(new DbContextOptionsBuilder<PostgreDbRepositoryContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();

    private readonly User _owner = new()
    {
        Id = "owner-1", UserName = "owner@test.com", NormalizedUserName = "OWNER@TEST.COM", Email = "owner@test.com",
        NormalizedEmail = "OWNER@TEST.COM", EmailConfirmed = true, PasswordHash = "secret-hash", RefreshToken = "secret-refresh"
    };

    private readonly PostgresPersonMirror _sut;

    public PostgresPersonMirrorTests()
    {
        _userManager.Setup(m => m.FindByIdAsync(_owner.Id)).ReturnsAsync(_owner);
        _sut = new PostgresPersonMirror(_mirror, _userManager.Object, NullLogger<PostgresPersonMirror>.Instance);
    }

    public void Dispose() => _mirror.Dispose();

    private Person NewPerson(string name = "Anna") => new()
    {
        Id = Guid.NewGuid(), Name = name, Surname = "Novak", DayOfBirth = new DateOnly(1990, 5, 1),
        DayOfNameDay = new DateOnly(2000, 7, 26), UserId = _owner.Id
    };

    [Fact]
    public async Task Upsert_OwnerNotCopiedYet_CopiesTheOwnerWithoutSecretsAndThePerson()
    {
        var person = NewPerson();

        await _sut.UpsertPersonAsync(person);

        var owner = await _mirror.Users.AsNoTracking().SingleAsync();
        owner.Id.Should().Be(_owner.Id);
        owner.Email.Should().Be(_owner.Email);
        owner.PasswordHash.Should().BeNull();
        owner.RefreshToken.Should().BeEmpty();
        var copy = await _mirror.Persons.AsNoTracking().SingleAsync();
        copy.Should().BeEquivalentTo(person, o => o.Excluding(p => p.User));
    }

    [Fact]
    public async Task Upsert_ExistingCopy_IsUpdated()
    {
        var person = NewPerson();
        await _sut.UpsertPersonAsync(person);

        person.Name = "Hana";
        person.DayOfBirth = null;
        await _sut.UpsertPersonAsync(person);

        var copy = await _mirror.Persons.AsNoTracking().SingleAsync();
        copy.Name.Should().Be("Hana");
        copy.DayOfBirth.Should().BeNull();
        (await _mirror.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Upsert_Failure_IsLoggedNotThrown()
    {
        _userManager.Setup(m => m.FindByIdAsync(_owner.Id)).ReturnsAsync((User?)null);

        var act = () => _sut.UpsertPersonAsync(NewPerson());

        await act.Should().NotThrowAsync();
        (await _mirror.Persons.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Deletes_NeverThrow()
    {
        // The in-memory provider can't run ExecuteDelete; the error must be logged, not thrown.
        await _sut.Invoking(s => s.DeletePersonAsync(Guid.NewGuid())).Should().NotThrowAsync();
        await _sut.Invoking(s => s.DeleteUserAsync(_owner.Id)).Should().NotThrowAsync();
    }
}
