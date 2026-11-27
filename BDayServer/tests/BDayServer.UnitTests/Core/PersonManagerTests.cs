using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Repository.DatabaseAccessInterfaces;
using Repository.Services;

namespace BDayServer.UnitTests.Core;

public class PersonManagerTests
{
    private const string UserName = "owner@test.com";

    private readonly Mock<IRepositoryManager> _repository = new();
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IGetUserProvider> _userProvider = new();
    private readonly Mock<IPersonMirror> _mirror = new();
    private readonly User _user = new() { Id = "user-1", UserName = UserName, Email = UserName };

    public PersonManagerTests()
    {
        _repository.Setup(r => r.Person).Returns(_personRepository.Object);
        _userProvider.Setup(p => p.UserName).Returns(UserName);
        _userManager.Setup(m => m.FindByNameAsync(UserName)).ReturnsAsync(_user);
    }

    private PersonManager CreateSut() =>
        new(_repository.Object, TestMapper.Create(), _userManager.Object, _userProvider.Object, _mirror.Object);

    private static PersonForCreationDto NewPerson() => new()
    {
        Name = "Anna",
        Surname = "Novak",
        DayOfBirth = new DateOnly(1990, 5, 1),
        DayOfNameDay = new DateOnly(2000, 7, 26)
    };

    private Person StoredPerson() => new()
    {
        Id = Guid.NewGuid(), Name = "Anna", DayOfNameDay = new DateOnly(2000, 7, 26), UserId = _user.Id
    };

    [Fact]
    public async Task GetPersonsAsync_Null_Throws()
    {
        var act = () => CreateSut().GetPersonsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetPersonsAsync_ReturnsTheRepositoryPage()
    {
        var parameters = new PersonParameters();
        var expected = new PagedList<PersonDto>([new PersonDto { Name = "Anna" }], 1, 1, 10);
        _personRepository.Setup(r => r.GetAllPersonsAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetPersonsAsync(parameters);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task CreatePersonAsync_NotSignedIn_Throws()
    {
        _userProvider.Setup(p => p.UserName).Returns((string?)null);

        var act = () => CreateSut().CreatePersonAsync(NewPerson());

        await act.Should().ThrowAsync<UserNotExistException>();
        _personRepository.Verify(r => r.CreatePerson(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task CreatePersonAsync_UnknownUser_Throws()
    {
        _userManager.Setup(m => m.FindByNameAsync(UserName)).ReturnsAsync((User?)null);

        var act = () => CreateSut().CreatePersonAsync(NewPerson());

        await act.Should().ThrowAsync<UserNotExistException>();
    }

    [Fact]
    public async Task CreatePersonAsync_SavesForTheSignedInUserThenCopiesToPostgreSql()
    {
        Person? created = null;
        _personRepository.Setup(r => r.CreatePerson(It.IsAny<Person>())).Callback<Person>(p => created = p);
        var order = new List<string>();
        _repository.Setup(r => r.SaveAsync()).Callback(() => order.Add("save")).Returns(Task.CompletedTask);
        _mirror.Setup(m => m.UpsertPersonAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("mirror")).Returns(Task.CompletedTask);

        var result = await CreateSut().CreatePersonAsync(NewPerson());

        created!.UserId.Should().Be(_user.Id);
        created.Name.Should().Be("Anna");
        result.Name.Should().Be("Anna");
        _mirror.Verify(m => m.UpsertPersonAsync(created, It.IsAny<CancellationToken>()), Times.Once);
        order.Should().Equal("save", "mirror");
    }

    [Fact]
    public async Task UpdatePersonAsync_NotFound_ThrowsAndCopiesNothing()
    {
        var act = () => CreateSut().UpdatePersonAsync(Guid.NewGuid(), new PersonForUpdateDto { Name = "X" });

        await act.Should().ThrowAsync<PersonNotExistException>();
        _mirror.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePersonAsync_UpdatesThenCopiesToPostgreSql()
    {
        var person = StoredPerson();
        _personRepository.Setup(r => r.GetPersonAsync(person.Id, true)).ReturnsAsync(person);

        await CreateSut().UpdatePersonAsync(person.Id, new PersonForUpdateDto
        {
            Name = "Hana", DayOfNameDay = new DateOnly(2000, 5, 31)
        });

        person.Name.Should().Be("Hana");
        _repository.Verify(r => r.SaveAsync(), Times.Once);
        _mirror.Verify(m => m.UpsertPersonAsync(person, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonAsync_NotFound_ThrowsAndCopiesNothing()
    {
        var act = () => CreateSut().DeletePersonAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<PersonNotExistException>();
        _mirror.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeletePersonAsync_DeletesThenRemovesTheCopy()
    {
        var person = StoredPerson();
        _personRepository.Setup(r => r.GetPersonAsync(person.Id, true)).ReturnsAsync(person);

        await CreateSut().DeletePersonAsync(person.Id);

        _personRepository.Verify(r => r.DeletePerson(person), Times.Once);
        _repository.Verify(r => r.SaveAsync(), Times.Once);
        _mirror.Verify(m => m.DeletePersonAsync(person.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
