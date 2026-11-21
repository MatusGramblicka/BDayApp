using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.DatabaseAccessInterfaces;
using Repository.Repositories;
using Repository.Services;

namespace BDayServer.UnitTests.Core;

public class PersonManagerTests : IDisposable
{
    private const string UserName = "owner@test.com";

    private readonly Mock<IRepositoryManager> _repository = new();
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IGetUserProvider> _userProvider = new();
    private readonly PostgreDbRepositoryContext _postgreContext;
    private readonly User _user = new() { Id = "user-1", UserName = UserName, Email = UserName, RefreshToken = "" };

    public PersonManagerTests()
    {
        _repository.Setup(r => r.Person).Returns(_personRepository.Object);
        _userProvider.Setup(p => p.UserName).Returns(UserName);
        _userManager.Setup(m => m.FindByNameAsync(UserName)).ReturnsAsync(_user);

        _postgreContext = new PostgreDbRepositoryContext(new DbContextOptionsBuilder<PostgreDbRepositoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _postgreContext.Users.Add(_user);
        _postgreContext.SaveChanges();
    }

    public void Dispose() => _postgreContext.Dispose();

    private PersonManager CreateSut() =>
        new(_repository.Object, TestMapper.Create(), _userManager.Object, _userProvider.Object, _postgreContext);

    private static PersonForCreationDto NewPerson() => new()
    {
        Name = "Anna",
        Surname = "Novak",
        DayOfBirth = new DateOnly(1990, 5, 1),
        DayOfNameDay = new DateOnly(2000, 7, 26)
    };

    [Fact]
    public void GetPersons_Null_Throws()
    {
        var act = () => CreateSut().GetPersons(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetPersons_DelegatesToRepositoryWithoutTracking()
    {
        var parameters = new PersonParameters();
        var expected = new PagedList<PersonDto>([new PersonDto { Name = "Anna" }], 1, 1, 10);
        _personRepository.Setup(r => r.GetAllPersonsAsync(parameters, false)).Returns(expected);

        var result = CreateSut().GetPersons(parameters);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task CreatePersonAsync_NoCurrentUser_Throws()
    {
        _userProvider.Setup(p => p.UserName).Returns((string)null!);

        var act = () => CreateSut().CreatePersonAsync(NewPerson());

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreatePersonAsync_UserNotFound_Throws()
    {
        _userManager.Setup(m => m.FindByNameAsync(UserName)).ReturnsAsync((User?)null);

        var act = () => CreateSut().CreatePersonAsync(NewPerson());

        await act.Should().ThrowAsync<UserNotExistException>();
        _repository.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task CreatePersonAsync_SavesToPrimaryAndPostgreWithSameId()
    {
        var generatedId = Guid.NewGuid();
        Person? created = null;
        _personRepository.Setup(r => r.CreatePerson(It.IsAny<Person>()))
            .Callback<Person>(p => { p.Id = generatedId; created = p; });

        var result = await CreateSut().CreatePersonAsync(NewPerson());

        created.Should().NotBeNull();
        created!.UserId.Should().Be(_user.Id);
        _repository.Verify(r => r.SaveAsync(), Times.Once);

        result.Id.Should().Be(generatedId);
        result.Name.Should().Be("Anna");

        var mirrored = await _postgreContext.Persons.SingleAsync();
        mirrored.Id.Should().Be(generatedId);
        mirrored.UserId.Should().Be(_user.Id);
        mirrored.Name.Should().Be("Anna");
        mirrored.DayOfBirth.Should().Be(new DateOnly(1990, 5, 1));
    }

    [Fact]
    public async Task UpdatePersonAsync_NotInPrimary_Throws()
    {
        var act = () => CreateSut().UpdatePersonAsync(Guid.NewGuid(), new PersonForUpdateDto { Name = "X" });

        await act.Should().ThrowAsync<PersonNotExistException>().WithMessage("Person does not exist");
    }

    [Fact]
    public async Task UpdatePersonAsync_NotInPostgre_Throws()
    {
        var id = Guid.NewGuid();
        _personRepository.Setup(r => r.GetPersonAsync(id, true)).ReturnsAsync(new Person { Id = id, Name = "Old" });

        var act = () => CreateSut().UpdatePersonAsync(id, new PersonForUpdateDto { Name = "New" });

        await act.Should().ThrowAsync<PersonNotExistException>().WithMessage("*PostgreSql*");
    }

    [Fact]
    public async Task UpdatePersonAsync_UpdatesBothStores()
    {
        var id = Guid.NewGuid();
        var primary = new Person { Id = id, Name = "Old", UserId = _user.Id };
        _personRepository.Setup(r => r.GetPersonAsync(id, true)).ReturnsAsync(primary);
        _postgreContext.Persons.Add(new Person { Id = id, Name = "Old", UserId = _user.Id });
        await _postgreContext.SaveChangesAsync();

        await CreateSut().UpdatePersonAsync(id, new PersonForUpdateDto
        {
            Name = "New",
            Surname = "Surname",
            DayOfNameDay = new DateOnly(2000, 1, 1)
        });

        primary.Name.Should().Be("New");
        primary.Surname.Should().Be("Surname");
        _repository.Verify(r => r.SaveAsync(), Times.Once);
        (await _postgreContext.Persons.SingleAsync(p => p.Id == id)).Name.Should().Be("New");
    }

    [Fact]
    public async Task DeletePersonAsync_NotInPrimary_Throws()
    {
        var act = () => CreateSut().DeletePersonAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<PersonNotExistException>();
        _personRepository.Verify(r => r.DeletePerson(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task DeletePersonAsync_DeletesFromBothStores()
    {
        var id = Guid.NewGuid();
        var primary = new Person { Id = id, Name = "Anna", UserId = _user.Id };
        _personRepository.Setup(r => r.GetPersonAsync(id, true)).ReturnsAsync(primary);
        _postgreContext.Persons.Add(new Person { Id = id, Name = "Anna", UserId = _user.Id });
        await _postgreContext.SaveChangesAsync();

        await CreateSut().DeletePersonAsync(id);

        _personRepository.Verify(r => r.DeletePerson(primary), Times.Once);
        _repository.Verify(r => r.SaveAsync(), Times.Once);
        (await _postgreContext.Persons.AnyAsync(p => p.Id == id)).Should().BeFalse();
    }
}
