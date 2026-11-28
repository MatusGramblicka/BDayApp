using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Contracts.DataTransferObjects.Event;
using Entities.RequestFeatures;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.HttpRepository;

public class EventHttpRepositoryTests
{
    private const string Api = "https://api.test/api/";

    private readonly MockHttpMessageHandler _http = new();
    private readonly EventHttpRepository _sut;

    public EventHttpRepositoryTests()
    {
        var client = _http.ToHttpClient();
        client.BaseAddress = new Uri(Api);
        _sut = new EventHttpRepository(client);
    }

    private static HttpResponseMessage EventsPage(MetaData? metaData, params object[] events)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(events) };
        if (metaData is not null)
            // The API writes the header with Newtonsoft, i.e. PascalCase names.
            response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metaData));
        return response;
    }

    [Fact]
    public async Task GetEvents_SendsQueryParameters_AndParsesItemsAndPaginationHeader()
    {
        var metaData = new MetaData { CurrentPage = 2, PageSize = 4, TotalCount = 9, TotalPages = 3 };
        _http.Expect(HttpMethod.Get, Api + "events")
            .WithExactQueryString(new Dictionary<string, string>
            {
                ["pageNumber"] = "2",
                ["pageSize"] = "4",
                ["searchTerm"] = "par",
                ["orderBy"] = "name desc"
            })
            .Respond(_ => EventsPage(metaData,
                new { id = Guid.NewGuid(), name = "Party", date = "2026-07-26T00:00:00", imageUrl = "StaticFiles/Images/a.png" },
                new { id = Guid.NewGuid(), name = "Trip", date = "2026-05-02T00:00:00" }));

        var result = await _sut.GetEvents(new EventParameters
        {
            PageNumber = 2, PageSize = 4, SearchTerm = "par", OrderBy = "name desc"
        }, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(e => e.Name).Should().Equal("Party", "Trip");
        result.Items[0].Date.Should().Be(new DateTime(2026, 7, 26));
        result.Items[0].ImageUrl.Should().Be("StaticFiles/Images/a.png");
        result.MetaData.Should().BeEquivalentTo(metaData);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetEvents_Defaults_SendDateAscendingAndEmptySearch()
    {
        _http.Expect(HttpMethod.Get, Api + "events")
            .WithQueryString("searchTerm", "")
            .WithQueryString("orderBy", "date ascending")
            .WithQueryString("pageSize", "10")
            .Respond(_ => EventsPage(new MetaData()));

        var result = await _sut.GetEvents(new EventParameters(), Xunit.TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetEvents_EmptyOrderBy_IsSentAsEmpty()
    {
        _http.Expect(HttpMethod.Get, Api + "events")
            .WithQueryString("orderBy", "")
            .Respond(_ => EventsPage(new MetaData()));

        await _sut.GetEvents(new EventParameters { OrderBy = "" }, Xunit.TestContext.Current.CancellationToken);

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetEvents_MissingPaginationHeader_FallsBackToASinglePage()
    {
        _http.When(HttpMethod.Get, Api + "events")
            .Respond(_ => EventsPage(null, new { id = Guid.NewGuid(), name = "Party", date = "2026-07-26T00:00:00" }));

        var result = await _sut.GetEvents(new EventParameters(), Xunit.TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.MetaData.TotalCount.Should().Be(1);
        result.MetaData.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task GetEvents_ServerError_ThrowsApiException()
    {
        _http.When(HttpMethod.Get, Api + "events").Respond(HttpStatusCode.InternalServerError);

        var act = () => _sut.GetEvents(new EventParameters());

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task GetEvents_BodyIsNotAList_ThrowsApiException()
    {
        _http.When(HttpMethod.Get, Api + "events").Respond("text/plain", "oops");

        var act = () => _sut.GetEvents(new EventParameters());

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task GetEvent_RequestsById()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"events/{id}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new { id, name = "Party", date = "2026-07-26T00:00:00" }));

        var ev = await _sut.GetEvent(id, Xunit.TestContext.Current.CancellationToken);

        ev.Should().NotBeNull();
        ev!.Id.Should().Be(id);
        ev.Name.Should().Be("Party");
        ev.Date.Should().Be(new DateTime(2026, 7, 26));
    }

    [Fact]
    public async Task GetEvent_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"events/{id}").Respond(HttpStatusCode.NotFound);

        var ev = await _sut.GetEvent(id, Xunit.TestContext.Current.CancellationToken);

        ev.Should().BeNull();
    }

    [Fact]
    public async Task GetEvent_ServerError_ThrowsApiException()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"events/{id}").Respond(HttpStatusCode.InternalServerError);

        var act = () => _sut.GetEvent(id);

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task GetEvent_EmptyBody_ThrowsApiException()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"events/{id}").Respond(HttpStatusCode.OK);

        var act = () => _sut.GetEvent(id);

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task CreateEvent_PostsDto()
    {
        _http.Expect(HttpMethod.Post, Api + "events")
            .WithJsonContent<EventForCreationDto>(
                e => e.Name == "Party" && e.Date == new DateTime(2026, 7, 26), TestJson.Web)
            .Respond(HttpStatusCode.Created);

        await _sut.CreateEvent(new EventForCreationDto { Name = "Party", Date = new DateTime(2026, 7, 26) });

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CreateEvent_Refused_ThrowsApiException()
    {
        _http.When(HttpMethod.Post, Api + "events").Respond(HttpStatusCode.UnprocessableEntity);

        var act = () => _sut.CreateEvent(new EventForCreationDto());

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task UpdateEvent_PutsToEventUrl()
    {
        var id = Guid.NewGuid();
        _http.Expect(HttpMethod.Put, Api + $"events/{id}")
            .WithJsonContent<EventForUpdateDto>(e => e.Name == "New", TestJson.Web)
            .Respond(HttpStatusCode.NoContent);

        await _sut.UpdateEvent(id, new EventForUpdateDto { Name = "New" });

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UpdateEvent_NotFound_ThrowsApiException()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Put, Api + $"events/{id}").Respond(HttpStatusCode.NotFound);

        var act = () => _sut.UpdateEvent(id, new EventForUpdateDto { Name = "New" });

        (await act.Should().ThrowAsync<ApiException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_DeletesEventUrl()
    {
        var id = Guid.NewGuid();
        _http.Expect(HttpMethod.Delete, Api + $"events/{id}").Respond(HttpStatusCode.NoContent);

        await _sut.DeleteEvent(id);

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeleteEvent_NotFound_ThrowsApiException()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Delete, Api + $"events/{id}").Respond(HttpStatusCode.NotFound);

        var act = () => _sut.DeleteEvent(id);

        await act.Should().ThrowAsync<ApiException>();
    }

}
