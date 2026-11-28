using System.Text.Json;
using BDayServer.Controllers;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.UnitTests.Controllers;

public class PersonsControllerTests
{
    private readonly Mock<IPersonManager> _manager = new();
    private readonly PersonsController _sut;

    public PersonsControllerTests()
    {
        _sut = new PersonsController(_manager.Object).WithHttpContext();
    }

    [Fact]
    public async Task GetPersons_ReturnsThePageAndThePaginationHeader()
    {
        var page = new PagedList<PersonDto>([new PersonDto { Name = "Anna" }], count: 11, pageNumber: 2, pageSize: 10);
        _manager.Setup(m => m.GetPersonsAsync(It.IsAny<PersonParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _sut.GetPersons(new PersonParameters(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
        // The client reads this header with the same MetaData type, so the property names must stay PascalCase.
        using var header = JsonDocument.Parse(_sut.Response.Headers["X-Pagination"].ToString());
        header.RootElement.GetProperty("CurrentPage").GetInt32().Should().Be(2);
        header.RootElement.GetProperty("TotalPages").GetInt32().Should().Be(2);
        header.RootElement.GetProperty("TotalCount").GetInt32().Should().Be(11);
        header.RootElement.GetProperty("HasPrevious").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetPerson_NotFound_Returns404()
    {
        var result = await _sut.GetPerson(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreatePerson_Success_Returns201AtThePersonsRoute()
    {
        var created = new PersonDto { Id = Guid.NewGuid(), Name = "Anna" };
        _manager.Setup(m => m.CreatePersonAsync(It.IsAny<PersonForCreationDto>())).ReturnsAsync(created);

        var result = await _sut.CreatePerson(new PersonForCreationDto { Name = "Anna" });

        var createdResult = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("PersonById");
        createdResult.Value.Should().BeSameAs(created);
    }

    [Fact]
    public async Task UpdatePerson_NotFound_Returns404()
    {
        _manager.Setup(m => m.UpdatePersonAsync(It.IsAny<Guid>(), It.IsAny<PersonForUpdateDto>()))
            .ThrowsAsync(new PersonNotExistException("Person does not exist"));

        var result = await _sut.UpdatePerson(Guid.NewGuid(), new PersonForUpdateDto());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePerson_NotFound_Returns404()
    {
        _manager.Setup(m => m.DeletePersonAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new PersonNotExistException("Person does not exist"));

        var result = await _sut.DeletePerson(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePerson_Success_Returns204()
    {
        var result = await _sut.DeletePerson(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }
}
