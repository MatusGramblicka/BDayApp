using BDayClient.Components;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Bunit.TestDoubles;
using Contracts.DataTransferObjects.Event;
using Microsoft.Extensions.DependencyInjection;

namespace BDayClient.UnitTests.Components;

public class EventComponentsTests : BunitContext
{
    private readonly BunitAuthorizationContext _auth;

    public EventComponentsTests()
    {
        Services.AddSingleton(new ApiUrls("https://api.test"));
        Services.AddSingleton(Mock.Of<IToastService>());
        Services.AddSingleton(Mock.Of<IPersonHttpRepository>());
        _auth = AddAuthorization();
        _auth.SetAuthorized("john@test.com");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static EventDto Dto(string name, int day = 26, string? imageUrl = null) => new()
    {
        Id = Guid.NewGuid(), Name = name, Date = new DateTime(2026, 7, day, 13, 45, 0), ImageUrl = imageUrl ?? ""
    };

    private static IEnumerable<string> VisibleMessages(IRenderedComponent<EventForm> cut) =>
        // Since .NET 11 every ValidationMessage renders a hidden placeholder; only visible ones are messages.
        cut.FindAll(".validation-message:not([hidden])").Select(m => m.TextContent.Trim());

    // ---- EventTable ----

    [Fact]
    public void EventTable_WhileLoading_SaysLoading()
    {
        var cut = Render<EventTable>(ps => ps.Add(p => p.Events, null));

        cut.Markup.Should().Contain("Loading events...");
        cut.FindAll("table").Should().BeEmpty();
    }

    [Fact]
    public void EventTable_NoEvents_SaysSo()
    {
        var cut = Render<EventTable>(ps => ps.Add(p => p.Events, []));

        cut.Markup.Should().Contain("No events found.").And.NotContain("Loading");
        cut.FindAll("table").Should().BeEmpty();
    }

    [Fact]
    public void EventTable_RendersRows_WithDateOnly_AndLinks()
    {
        var first = Dto("Party");
        var second = Dto("Trip", 27);

        var cut = Render<EventTable>(ps => ps.Add(p => p.Events, new List<EventDto> { first, second }));

        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(2);
        rows[0].QuerySelectorAll("td")[0].TextContent.Trim().Should().Be("Party");
        // Without the year, like a person's birthday.
        rows[0].QuerySelectorAll("td")[1].TextContent.Trim().Should().Be("26.07.");
        rows[1].QuerySelectorAll("td")[1].TextContent.Trim().Should().Be("27.07.");
        rows[0].QuerySelector("a[href='/event/" + first.Id + "']").Should().NotBeNull();
        rows[0].QuerySelector("a.btn-info")!.GetAttribute("href").Should().Be("/updateEvent/" + first.Id);
    }

    [Fact]
    public void EventTable_Images_OnlyForAdministrator_AndResolvedAgainstTheServer()
    {
        var events = new List<EventDto>
        {
            Dto("Party", imageUrl: "StaticFiles/Images/pic.png"),
            Dto("Trip", imageUrl: "https://old.test/StaticFiles/Images/old.png"),
            Dto("Plain")
        };

        var user = Render<EventTable>(ps => ps.Add(p => p.Events, events));
        user.FindAll("th").Select(th => th.TextContent).Should().NotContain("Image");
        user.FindAll("img").Should().BeEmpty();

        _auth.SetRoles("Administrator");
        var admin = Render<EventTable>(ps => ps.Add(p => p.Events, events));
        admin.FindAll("th").Select(th => th.TextContent).Should().Contain("Image");
        admin.FindAll("img").Select(i => i.GetAttribute("src")).Should().Equal(
            "https://api.test/StaticFiles/Images/pic.png", "https://old.test/StaticFiles/Images/old.png");
    }

    [Fact]
    public void EventTable_Delete_AsksForConfirmation_ThenRaisesOnDelete()
    {
        var target = Dto("Party");
        var deleted = new List<Guid>();
        var cut = Render<EventTable>(ps => ps
            .Add(p => p.Events, new List<EventDto> { Dto("Other"), target })
            .Add(p => p.OnDelete, id => deleted.Add(id)));

        cut.FindAll("tbody button.btn-danger")[1].Click();

        deleted.Should().BeEmpty("the deletion must be confirmed first");
        cut.Find(".modal").GetAttribute("style").Should().Contain("display:block");

        cut.Find(".modal-footer button.btn-success").Click();

        deleted.Should().Equal(target.Id);
        cut.Find(".modal").GetAttribute("style").Should().Contain("display:none");
    }

    [Fact]
    public void EventTable_DeleteCancelled_DoesNotRaiseOnDelete()
    {
        var deleted = new List<Guid>();
        var cut = Render<EventTable>(ps => ps
            .Add(p => p.Events, new List<EventDto> { Dto("Party") })
            .Add(p => p.OnDelete, id => deleted.Add(id)));

        cut.Find("tbody button.btn-danger").Click();
        cut.Find(".modal-footer button.btn-danger").Click();

        deleted.Should().BeEmpty();
    }

    // ---- EventSort ----

    [Theory]
    [InlineData("name desc")]
    [InlineData("date desc")]
    public void EventSort_SelectionChange_RaisesOrderBy(string orderBy)
    {
        string? raised = null;
        var cut = Render<EventSort>(ps => ps.Add(p => p.OnSortChanged, value => raised = value));

        cut.Find("select").Change(orderBy);

        raised.Should().Be(orderBy);
    }

    [Fact]
    public void EventSort_DefaultSelection_IsDateAscending()
    {
        var cut = Render<EventSort>();

        cut.Find("option[selected]").GetAttribute("value").Should().Be("date asc");
    }

    // ---- Search ----

    [Fact]
    public void Search_PlaceholderDefaultsToPersons_AndCanBeChanged()
    {
        Render<Search>().Find("input").GetAttribute("placeholder").Should().Be("Search by person name");

        var cut = Render<Search>(ps => ps.Add(p => p.Placeholder, "Search by event name"));

        cut.Find("input").GetAttribute("placeholder").Should().Be("Search by event name");
        cut.Find("input").GetAttribute("aria-label").Should().Be("Search by event name");
    }

    // ---- EventForm ----

    private IRenderedComponent<EventForm> RenderForm(EventForManipulation model, Action? onSubmit = null) =>
        Render<EventForm>(ps => ps
            .Add(p => p.Model, model)
            .Add(p => p.SubmitText, "Save it")
            .Add(p => p.OnValidSubmit, () => onSubmit?.Invoke()));

    private static bool SubmitDisabled(IRenderedComponent<EventForm> cut) =>
        cut.Find("button[type=submit]").HasAttribute("disabled");

    [Fact]
    public void EventForm_RendersFields_AndSubmitText()
    {
        var cut = RenderForm(new EventForManipulation { Name = "Party", Date = new DateOnly(2026, 7, 26) });

        cut.Find("input#name").GetAttribute("value").Should().Be("Party");
        // Day and month pickers, no year - like a person's birthday and name day.
        cut.Find("select[aria-label='Month']").GetAttribute("value").Should().Be("7");
        cut.Find("select[aria-label='Day']").GetAttribute("value").Should().Be("26");
        cut.FindAll("input[type=date]").Should().BeEmpty();
        cut.Find("button[type=submit]").TextContent.Trim().Should().Be("Save it");
    }

    [Fact]
    public void EventForm_SubmitIsDisabledUntilSomethingIsChanged_AndInputIsValid()
    {
        var cut = RenderForm(new EventForManipulation { Date = new DateOnly(2026, 7, 26) });
        SubmitDisabled(cut).Should().BeTrue("nothing was changed yet");

        cut.Find("input#name").Change(new string('x', 31));
        SubmitDisabled(cut).Should().BeTrue("the name is too long");
        VisibleMessages(cut).Should().Equal("Maximum length for the Name is 30 characters.");

        cut.Find("input#name").Change("Party");
        SubmitDisabled(cut).Should().BeFalse();
        VisibleMessages(cut).Should().BeEmpty();
    }

    [Fact]
    public void EventForm_ChangingTheDate_EnablesSubmit()
    {
        var cut = RenderForm(new EventForManipulation { Name = "Party", Date = new DateOnly(2026, 7, 26) });

        cut.Find("select[aria-label='Month']").Change("8");

        SubmitDisabled(cut).Should().BeFalse();
    }

    [Fact]
    public void EventForm_ValidSubmit_UpdatesTheModel_AndRaisesOnValidSubmit()
    {
        var model = new EventForManipulation { Name = "Party", Date = new DateOnly(2026, 7, 26) };
        var submits = 0;
        var cut = RenderForm(model, () => submits++);

        cut.Find("input#name").Change("Trip");
        cut.Find("select[aria-label='Month']").Change("8");
        cut.Find("select[aria-label='Day']").Change("1");
        cut.Find("form").Submit();

        submits.Should().Be(1);
        model.Name.Should().Be("Trip");
        // The pickers store day and month in the year 2000, as for persons.
        model.Date.Should().Be(new DateOnly(2000, 8, 1));
    }

    [Fact]
    public void EventForm_InvalidSubmit_ShowsMessage_AndDoesNotRaiseOnValidSubmit()
    {
        var submits = 0;
        var cut = RenderForm(new EventForManipulation { Date = new DateOnly(2026, 7, 26) }, () => submits++);

        cut.Find("form").Submit();

        submits.Should().Be(0);
        VisibleMessages(cut).Should().Equal("Name is a required field.");
    }

    [Fact]
    public void EventForm_NewModelInstance_StartsOverWithAnUntouchedForm()
    {
        var cut = RenderForm(new EventForManipulation { Name = "Party" });
        cut.Find("input#name").Change("Trip");
        SubmitDisabled(cut).Should().BeFalse();

        cut.Render(ps => ps
            .Add(p => p.Model, new EventForManipulation { Date = new DateOnly(2026, 7, 26) })
            .Add(p => p.SubmitText, "Save it"));

        SubmitDisabled(cut).Should().BeTrue();
        VisibleMessages(cut).Should().BeEmpty();
    }

    [Fact]
    public void EventForm_ImageUpload_OnlyForAdministrator()
    {
        var user = RenderForm(new EventForManipulation { Name = "Party" });
        user.FindAll("input[type=file]").Should().BeEmpty();

        _auth.SetRoles("Administrator");
        var admin = RenderForm(new EventForManipulation { Name = "Party", ImageUrl = "StaticFiles/Images/pic.png" });
        admin.FindAll("input[type=file]").Should().ContainSingle();
        admin.Find("img.image-preview").GetAttribute("src").Should().Be("https://api.test/StaticFiles/Images/pic.png");
    }

    // ---- Mapping ----

    [Fact]
    public void EventForCreation_ToDto_SendsTheDateAtMidnight_AndEmptyImageAsNull()
    {
        var dto = new EventForCreation { Name = "Party", Date = new DateOnly(2026, 7, 26), ImageUrl = " " }.ToDto();

        dto.Name.Should().Be("Party");
        dto.Date.Should().Be(new DateTime(2026, 7, 26, 0, 0, 0));
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void EventForUpdate_FromDto_DropsTheTime_AndToDtoRestoresMidnight()
    {
        var form = EventForUpdate.FromDto(Dto("Party", imageUrl: "StaticFiles/Images/a.png"));

        form.Date.Should().Be(new DateOnly(2026, 7, 26));
        form.ImageUrl.Should().Be("StaticFiles/Images/a.png");
        form.ToDto().Date.Should().Be(new DateTime(2026, 7, 26, 0, 0, 0));
        form.Copy().Should().NotBeSameAs(form).And.BeEquivalentTo(form);
    }
}
