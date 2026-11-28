using BDayClient.Components;
using System.Net;
using BDayClient.Features;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pages;
using Blazored.Toast.Configuration;
using Blazored.Toast.Services;
using Bunit.TestDoubles;
using Contracts.DataTransferObjects.Event;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BDayClient.UnitTests.Pages;

public class EventPagesTests : BunitContext
{
    private readonly Mock<IEventHttpRepository> _repo = new();
    private readonly Mock<IToastService> _toasts = new();
    private readonly BunitAuthorizationContext _authorization;

    // EventParameters is mutated by the page, so record a snapshot of each request.
    private readonly List<(int PageNumber, int PageSize, string? OrderBy, string? SearchTerm)> _requests = [];

    public EventPagesTests()
    {
        Services.AddSingleton(_repo.Object);
        Services.AddSingleton(_toasts.Object);
        Services.AddSingleton(new ApiUrls("https://api.test"));
        Services.AddSingleton(Mock.Of<IPersonHttpRepository>());
        _authorization = AddAuthorization();
        _authorization.SetAuthorized("john@test.com");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static EventDto Dto(string name, int day = 26) => new()
    {
        Id = Guid.NewGuid(), Name = name, Date = new DateTime(2026, 7, day), ImageUrl = ""
    };

    private static PagingResponse<EventDto> Page(List<EventDto> items, int currentPage = 1, int totalPages = 1) => new()
    {
        Items = items,
        MetaData = new MetaData
            { CurrentPage = currentPage, TotalPages = totalPages, PageSize = 10, TotalCount = items.Count }
    };

    private static ApiException ServerError(string message, bool handled = false) =>
        new(new ApiError(HttpStatusCode.BadRequest, [message], handled));

    private string CurrentUri => Services.GetRequiredService<NavigationManager>().Uri;

    private void SetupPaged(List<EventDto> items, int currentPage, int totalPages)
        => _repo.Setup(r => r.GetEvents(It.IsAny<EventParameters>(), It.IsAny<CancellationToken>()))
            .Callback<EventParameters, CancellationToken>((p, _) =>
                _requests.Add((p.PageNumber, p.PageSize, p.OrderBy, p.SearchTerm)))
            .ReturnsAsync(() => Page(items, currentPage, totalPages));

    private void SetupList(params EventDto[] items) => SetupPaged([.. items], 1, 1);

    private void VerifySuccessToast(string message) =>
        _toasts.Verify(t => t.ShowSuccess(message, It.IsAny<Action<ToastSettings>?>()), Times.Once);

    private void VerifyNoSuccessToast() =>
        _toasts.Verify(t => t.ShowSuccess(It.IsAny<string>(), It.IsAny<Action<ToastSettings>?>()), Times.Never);

    private void VerifyErrorToast(string message) =>
        _toasts.Verify(t => t.ShowError(message, It.IsAny<Action<ToastSettings>?>()), Times.Once);

    private void VerifyNoErrorToast() =>
        _toasts.Verify(t => t.ShowError(It.IsAny<string>(), It.IsAny<Action<ToastSettings>?>()), Times.Never);

    private static void ConfirmDelete(IRenderedComponent<Events> cut)
    {
        cut.Find("tbody button.btn-danger").Click();
        cut.Find(".modal-footer button.btn-success").Click();
    }

    // ---- Events list ----

    [Fact]
    public void Events_LoadsAndRendersTable_WithDefaultParameters()
    {
        SetupList(Dto("Party"), Dto("Trip"));

        var cut = Render<Events>();

        cut.FindAll("tbody tr").Should().HaveCount(2);
        _requests.Should().Equal((1, 10, "date asc", null));
        cut.Find("a.btn-success").GetAttribute("href").Should().Be("/createEvent");
        cut.FindComponent<Search>().Find("input").GetAttribute("placeholder").Should().Be("Search by event name");
    }

    [Fact]
    public void Events_ShowsLoadingMessage_WhileRequestIsPending()
    {
        var pending = new TaskCompletionSource<PagingResponse<EventDto>>();
        _repo.Setup(r => r.GetEvents(It.IsAny<EventParameters>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        var cut = Render<Events>();

        cut.Markup.Should().Contain("Loading events...");

        pending.SetResult(Page([Dto("Party")]));
        cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading events..."));
        cut.FindAll("tbody tr").Should().ContainSingle();
    }

    [Fact]
    public void Events_Empty_SaysNoEventsFound_NotLoading()
    {
        SetupList();

        var cut = Render<Events>();

        cut.Markup.Should().Contain("No events found.").And.NotContain("Loading");
    }

    [Fact]
    public void Events_LoadFails_ShowsFailedState_AndTheServerMessage()
    {
        _repo.Setup(r => r.GetEvents(It.IsAny<EventParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ServerError("Boom"));

        var cut = Render<Events>();

        cut.Markup.Should().Contain("The events could not be loaded.").And.NotContain("Loading");
        VerifyErrorToast("Boom");
    }

    [Fact]
    public void Events_LoadFailureAlreadyShownCentrally_ShowsNoSecondToast()
    {
        _repo.Setup(r => r.GetEvents(It.IsAny<EventParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ServerError("Too many requests", handled: true));

        Render<Events>();

        VerifyNoErrorToast();
    }

    [Fact]
    public void Events_ReloadFails_KeepsTheListShown()
    {
        _repo.SetupSequence(r => r.GetEvents(It.IsAny<EventParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page([Dto("Party")]))
            .ThrowsAsync(ServerError("Boom"));
        var cut = Render<Events>();

        cut.FindComponent<EventSort>().Find("select").Change("name desc");

        cut.FindAll("tbody tr").Should().ContainSingle();
        cut.Markup.Should().NotContain("could not be loaded");
        VerifyErrorToast("Boom");
    }

    [Fact]
    public void Events_SortChange_ReloadsWithOrderBy()
    {
        SetupList(Dto("Party"));
        var cut = Render<Events>();

        cut.FindComponent<EventSort>().Find("select").Change("name desc");

        _requests.Should().Equal((1, 10, "date asc", null), (1, 10, "name desc", null));
    }

    [Fact]
    public void Events_SortByDayOfYear_SendsAnEmptyOrderBy()
    {
        SetupList(Dto("Party"));
        var cut = Render<Events>();

        cut.FindComponent<EventSort>().Find("select").Change("");

        _requests.Last().OrderBy.Should().BeEmpty();
    }

    [Fact]
    public void Events_PageSizeChange_ResetsToFirstPage()
    {
        SetupList(Dto("Party"));
        var cut = Render<Events>();

        cut.FindComponent<PageSizeDropDown>().Find("select").Change("4");

        _requests.Last().Should().Be((1, 4, "date asc", null));
        _requests.Should().HaveCount(2);
    }

    [Fact]
    public void Events_SelectedPage_ReloadsThatPage()
    {
        SetupPaged([Dto("Party")], 1, 3);
        var cut = Render<Events>();

        cut.FindComponent<Pagination>().FindAll("li button").Single(b => b.TextContent.Trim() == "2").Click();

        _requests.Last().Should().Be((2, 10, "date asc", null));
        _requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Events_Search_ReloadsWithSearchTerm_FromFirstPage()
    {
        SetupList(Dto("Party"));
        var cut = Render<Events>();

        await cut.InvokeAsync(() => cut.FindComponent<Search>().Instance.OnSearchChanged.InvokeAsync("par"));

        _requests.Last().Should().Be((1, 10, "date asc", "par"));
    }

    [Fact]
    public void Events_Delete_CallsRepository_ShowsToast_AndReloads()
    {
        var target = Dto("Party");
        SetupList(target);
        _repo.Setup(r => r.DeleteEvent(target.Id)).Returns(Task.CompletedTask);
        var cut = Render<Events>();

        ConfirmDelete(cut);

        _repo.Verify(r => r.DeleteEvent(target.Id), Times.Once);
        VerifySuccessToast("Event \"Party\" was deleted.");
        _requests.Should().HaveCount(2);
    }

    [Fact]
    public void Events_DeleteFails_ShowsServerMessage_NoSuccessToast_AndDoesNotReload()
    {
        var target = Dto("Party");
        SetupList(target);
        _repo.Setup(r => r.DeleteEvent(target.Id)).ThrowsAsync(ServerError("Event does not exist"));
        var cut = Render<Events>();

        ConfirmDelete(cut);

        VerifyErrorToast("Event does not exist");
        VerifyNoSuccessToast();
        _requests.Should().HaveCount(1);
    }

    [Fact]
    public void Events_DeletingLastItemOnLaterPage_GoesToPreviousPage()
    {
        var target = Dto("Party");
        SetupPaged([target], 1, 3);
        _repo.Setup(r => r.DeleteEvent(target.Id)).Returns(Task.CompletedTask);
        var cut = Render<Events>();
        cut.FindComponent<Pagination>().FindAll("li button").Single(b => b.TextContent.Trim() == "2").Click();
        _requests.Last().PageNumber.Should().Be(2);

        ConfirmDelete(cut);

        _repo.Verify(r => r.DeleteEvent(target.Id), Times.Once);
        _requests.Select(r => r.PageNumber).Should().Equal(1, 2, 1);
    }

    // ---- EventDetails ----

    [Fact]
    public void EventDetails_ShowsNameAndDateOnly()
    {
        var ev = Dto("Party");
        _repo.Setup(r => r.GetEvent(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var cut = Render<EventDetails>(ps => ps.Add(p => p.Id, ev.Id));

        cut.Find(".event-title").TextContent.Should().Be("Party");
        cut.Find(".event-description").TextContent.Should().Be("Date: 26.07.");
        cut.Find("a.btn-success").GetAttribute("href").Should().Be("/events");
    }

    [Fact]
    public void EventDetails_ShowsTheImage_OnlyToAdministrators()
    {
        var ev = Dto("Party") with { ImageUrl = "StaticFiles/Images/a.png" };
        _repo.Setup(r => r.GetEvent(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        Render<EventDetails>(ps => ps.Add(p => p.Id, ev.Id)).FindAll("img").Should().BeEmpty();

        _authorization.SetRoles("Administrator");
        var admin = Render<EventDetails>(ps => ps.Add(p => p.Id, ev.Id));

        admin.Find("img.event-image").GetAttribute("src").Should().Be("https://api.test/StaticFiles/Images/a.png");
    }

    [Fact]
    public void EventDetails_UnknownEvent_GoesTo404()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).ReturnsAsync((EventDto?)null);

        Render<EventDetails>(ps => ps.Add(p => p.Id, id));

        CurrentUri.Should().EndWith("/404");
    }

    [Fact]
    public void EventDetails_LoadFails_ShowsFailedState_AndTheServerMessage()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).ThrowsAsync(ServerError("Boom"));

        var cut = Render<EventDetails>(ps => ps.Add(p => p.Id, id));

        cut.Markup.Should().Contain("The event could not be loaded.");
        VerifyErrorToast("Boom");
    }

    [Fact]
    public void EventDetails_ShowsLoadingMessage_WhileRequestIsPending()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).Returns(new TaskCompletionSource<EventDto?>().Task);

        var cut = Render<EventDetails>(ps => ps.Add(p => p.Id, id));

        cut.Markup.Should().Contain("Loading event...");
    }

    // ---- CreateEvent ----

    [Fact]
    public void CreateEvent_DefaultsDateToToday_AndDisablesSubmitUntilChanged()
    {
        var cut = Render<CreateEvent>();

        cut.Find("select[aria-label='Month']").GetAttribute("value").Should().Be($"{DateTime.Today.Month}");
        cut.Find("select[aria-label='Day']").GetAttribute("value").Should().Be($"{DateTime.Today.Day}");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void CreateEvent_Submit_SendsDateWithMidnightTime_ShowsToast_AndResetsForm()
    {
        EventForCreationDto? sent = null;
        _repo.Setup(r => r.CreateEvent(It.IsAny<EventForCreationDto>()))
            .Callback<EventForCreationDto>(dto => sent = dto)
            .Returns(Task.CompletedTask);
        var cut = Render<CreateEvent>();

        cut.Find("input#name").Change("Party");
        cut.Find("select[aria-label='Month']").Change("7");
        cut.Find("select[aria-label='Day']").Change("26");
        cut.Find("form").Submit();

        // Day and month in the year 2000, as for persons.
        sent.Should().BeEquivalentTo(new EventForCreationDto
            { Name = "Party", Date = new DateTime(2000, 7, 26, 0, 0, 0), ImageUrl = null });
        VerifySuccessToast("Event \"Party\" was added.");
        cut.Find("input#name").GetAttribute("value").Should().BeNullOrEmpty();
        cut.Find("select[aria-label='Month']").GetAttribute("value").Should().Be($"{DateTime.Today.Month}");
        cut.FindAll(".validation-message:not([hidden])").Should().BeEmpty();
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void CreateEvent_SubmittedWithoutName_ShowsTheValidationMessage_AndSendsNothing()
    {
        var cut = Render<CreateEvent>();

        cut.Find("form").Submit();

        cut.FindAll(".validation-message:not([hidden])").Select(m => m.TextContent.Trim())
            .Should().Equal("Name is a required field.");
        _repo.Verify(r => r.CreateEvent(It.IsAny<EventForCreationDto>()), Times.Never);
    }

    [Fact]
    public void CreateEvent_Refused_ShowsServerMessage_KeepsTheInput_AndNoSuccessToast()
    {
        _repo.Setup(r => r.CreateEvent(It.IsAny<EventForCreationDto>()))
            .ThrowsAsync(ServerError("Maximum length for the Name is 30 characters."));
        var cut = Render<CreateEvent>();

        cut.Find("input#name").Change("Party");
        cut.Find("form").Submit();

        VerifyErrorToast("Maximum length for the Name is 30 characters.");
        VerifyNoSuccessToast();
        cut.Find("input#name").GetAttribute("value").Should().Be("Party");
    }

    [Fact]
    public void CreateEvent_FailureAlreadyShownCentrally_ShowsNoSecondToast()
    {
        _repo.Setup(r => r.CreateEvent(It.IsAny<EventForCreationDto>()))
            .ThrowsAsync(ServerError("Too many requests", handled: true));
        var cut = Render<CreateEvent>();

        cut.Find("input#name").Change("Party");
        cut.Find("form").Submit();

        VerifyNoErrorToast();
        VerifyNoSuccessToast();
    }

    // ---- UpdateEvent ----

    [Fact]
    public void UpdateEvent_WhileLoading_ShowsLoadingInsteadOfTheForm()
    {
        var id = Guid.NewGuid();
        var pending = new TaskCompletionSource<EventDto?>();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).Returns(pending.Task);

        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, id));

        cut.Markup.Should().Contain("Loading event...");
        cut.FindAll("form").Should().BeEmpty();

        pending.SetResult(Dto("Party") with { Id = id });
        cut.WaitForAssertion(() => cut.Find("input#name").GetAttribute("value").Should().Be("Party"));
    }

    [Fact]
    public void UpdateEvent_LoadsEventIntoForm_WithDateOnly()
    {
        var ev = Dto("Party");
        _repo.Setup(r => r.GetEvent(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, ev.Id));

        cut.Find("input#name").GetAttribute("value").Should().Be("Party");
        cut.Find("select[aria-label='Month']").GetAttribute("value").Should().Be("7");
        cut.Find("select[aria-label='Day']").GetAttribute("value").Should().Be("26");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue("nothing was changed yet");
    }

    [Fact]
    public void UpdateEvent_Submit_SendsUpdate_ShowsToast_AndIsUntouchedAgain()
    {
        var ev = Dto("Party");
        _repo.Setup(r => r.GetEvent(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        EventForUpdateDto? sent = null;
        _repo.Setup(r => r.UpdateEvent(ev.Id, It.IsAny<EventForUpdateDto>()))
            .Callback<Guid, EventForUpdateDto>((_, dto) => sent = dto)
            .Returns(Task.CompletedTask);
        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, ev.Id));

        cut.Find("input#name").Change("Renamed");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeFalse();
        cut.Find("select[aria-label='Month']").Change("8");
        cut.Find("select[aria-label='Day']").Change("1");
        cut.Find("form").Submit();

        sent.Should().BeEquivalentTo(new EventForUpdateDto
            { Name = "Renamed", Date = new DateTime(2000, 8, 1, 0, 0, 0), ImageUrl = null });
        VerifySuccessToast("Event \"Renamed\" was updated.");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();
        cut.Find("input#name").GetAttribute("value").Should().Be("Renamed");
    }

    [Fact]
    public void UpdateEvent_SaveRefused_ShowsServerMessage_AndNoSuccessToast()
    {
        var ev = Dto("Party");
        _repo.Setup(r => r.GetEvent(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _repo.Setup(r => r.UpdateEvent(ev.Id, It.IsAny<EventForUpdateDto>()))
            .ThrowsAsync(ServerError("Event does not exist"));
        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, ev.Id));

        cut.Find("input#name").Change("Renamed");
        cut.Find("form").Submit();

        VerifyErrorToast("Event does not exist");
        VerifyNoSuccessToast();
        cut.Find("input#name").GetAttribute("value").Should().Be("Renamed", "the input is kept for another try");
    }

    [Fact]
    public void UpdateEvent_UnknownEvent_GoesTo404()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).ReturnsAsync((EventDto?)null);

        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, id));

        CurrentUri.Should().EndWith("/404");
        cut.FindAll("form").Should().BeEmpty();
    }

    [Fact]
    public void UpdateEvent_LoadFails_ShowsFailedState_AndTheServerMessage()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetEvent(id, It.IsAny<CancellationToken>())).ThrowsAsync(ServerError("Boom"));

        var cut = Render<UpdateEvent>(ps => ps.Add(p => p.Id, id));

        cut.Markup.Should().Contain("The event could not be loaded.");
        VerifyErrorToast("Boom");
    }

    // ---- Page contract ----

    [Theory]
    [InlineData(typeof(Events), "/events")]
    [InlineData(typeof(EventDetails), "/event/{id:guid}")]
    [InlineData(typeof(CreateEvent), "/createEvent")]
    [InlineData(typeof(UpdateEvent), "/updateEvent/{id:guid}")]
    public void Pages_RequireAuthorization_AndHaveTheExpectedRoute(Type page, string template)
    {
        page.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Should().NotBeEmpty();
        page.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Select(r => r.Template)
            .Should().Contain(template);
    }

    [Fact]
    public void Pages_SetThePageTitle()
    {
        SetupList(Dto("Party"));

        Render<Events>().FindComponents<Microsoft.AspNetCore.Components.Web.PageTitle>().Should().ContainSingle();
    }
}
