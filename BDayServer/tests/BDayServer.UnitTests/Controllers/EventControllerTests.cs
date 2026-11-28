using BDayServer.Controllers;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Event;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Repository.DatabaseAccessInterfaces;
using Repository.Services;

namespace BDayServer.UnitTests.Controllers;

public class EventControllerTests
{
    private const string UserName = "owner@test.com";

    private readonly Mock<IRepositoryManager> _repository = new();
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IGetUserProvider> _userProvider = new();
    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly EventController _sut;

    public EventControllerTests()
    {
        _repository.Setup(r => r.Event).Returns(_eventRepository.Object);
        _userProvider.Setup(p => p.UserName).Returns(UserName);
        _userManager.Setup(m => m.FindByNameAsync(UserName)).ReturnsAsync(new User { Id = "user-1", UserName = UserName });
        _sut = new EventController(_repository.Object, _userProvider.Object, _userManager.Object).WithHttpContext();
    }

    private static Event StoredEvent() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Party",
        Date = new DateTime(2026, 12, 24, 18, 0, 0),
        ImageUrl = "StaticFiles/Images/party.png",
        UserId = "user-1"
    };

    [Fact]
    public async Task GetEvents_ReturnsTheEventsAsDtos()
    {
        var stored = StoredEvent();
        _eventRepository.Setup(r => r.GetAllEventsAsync(It.IsAny<EventParameters>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<Event>([stored], count: 1, pageNumber: 1, pageSize: 10));

        var result = await _sut.GetEvents(new EventParameters(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<List<EventDto>>()
            .Which.Should().Equal(new EventDto
            {
                Id = stored.Id, Name = "Party", Date = stored.Date, ImageUrl = "StaticFiles/Images/party.png"
            });
        _sut.Response.Headers.ContainsKey("X-Pagination").Should().BeTrue();
    }

    [Fact]
    public async Task GetEvent_ReturnsTheEventAsDto()
    {
        var stored = StoredEvent();
        _eventRepository.Setup(r => r.GetEventAsync(stored.Id, false)).ReturnsAsync(stored);

        var result = await _sut.GetEvent(stored.Id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new EventDto
        {
            Id = stored.Id, Name = "Party", Date = stored.Date, ImageUrl = "StaticFiles/Images/party.png"
        });
    }

    [Fact]
    public async Task CreateEvent_SavesTheEventForTheSignedInUser_Returns201WithTheDto()
    {
        Event? created = null;
        _eventRepository.Setup(r => r.CreateEvent(It.IsAny<Event>())).Callback<Event>(e => created = e);

        var result = await _sut.CreateEvent(new EventForCreationDto { Name = "Trip", Date = new DateTime(2027, 1, 2) });

        created.Should().BeEquivalentTo(new
        {
            Name = "Trip", Date = new DateTime(2027, 1, 2), ImageUrl = (string?)null, UserId = "user-1"
        });
        _repository.Verify(r => r.SaveAsync(), Times.Once);
        var createdResult = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("EventById");
        createdResult.Value.Should().BeEquivalentTo(new
        {
            created!.Id, Name = "Trip", Date = new DateTime(2027, 1, 2), ImageUrl = (string?)null
        });
    }

    [Fact]
    public async Task UpdateEvent_OverwritesTheEventAndSaves()
    {
        var stored = StoredEvent();
        _eventRepository.Setup(r => r.GetEventAsync(stored.Id, true)).ReturnsAsync(stored);

        var result = await _sut.UpdateEvent(stored.Id,
            new EventForUpdateDto { Name = "Wedding", Date = new DateTime(2027, 6, 1), ImageUrl = null });

        result.Should().BeOfType<NoContentResult>();
        stored.Should().BeEquivalentTo(new
        {
            Name = "Wedding", Date = new DateTime(2027, 6, 1), ImageUrl = (string?)null, UserId = "user-1"
        });
        _repository.Verify(r => r.SaveAsync(), Times.Once);
    }
}
