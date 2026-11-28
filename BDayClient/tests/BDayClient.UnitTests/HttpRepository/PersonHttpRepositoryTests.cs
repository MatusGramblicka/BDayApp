using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.HttpRepository;

public class PersonHttpRepositoryTests
{
    private const string Api = "https://api.test/api/";

    private readonly MockHttpMessageHandler _http = new();
    private readonly PersonHttpRepository _sut;

    public PersonHttpRepositoryTests()
    {
        var client = _http.ToHttpClient();
        client.BaseAddress = new Uri(Api);
        _sut = new PersonHttpRepository(client);
    }

    private static HttpResponseMessage PersonsPage(MetaData? metaData, params object[] persons)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(persons) };
        if (metaData is not null)
            // The API writes the header with Newtonsoft, i.e. PascalCase names.
            response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metaData));
        return response;
    }

    [Fact]
    public async Task GetPersons_SendsQueryParameters_AndParsesItemsAndPaginationHeader()
    {
        var metaData = new MetaData { CurrentPage = 2, PageSize = 4, TotalCount = 9, TotalPages = 3 };
        _http.Expect(HttpMethod.Get, Api + "persons")
            .WithExactQueryString(new Dictionary<string, string>
            {
                ["pageNumber"] = "2",
                ["pageSize"] = "4",
                ["searchTerm"] = "an",
                ["orderBy"] = "name desc"
            })
            .Respond(_ => PersonsPage(metaData,
                new { id = Guid.NewGuid(), name = "Anna", dayOfNameDay = "2000-07-26", imageUrl = "StaticFiles/Images/a.png" },
                new { id = Guid.NewGuid(), name = "Boris", dayOfNameDay = "2000-05-02" }));

        var result = await _sut.GetPersons(new PersonParameters
        {
            PageNumber = 2, PageSize = 4, SearchTerm = "an", OrderBy = "name desc"
        }, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(p => p.Name).Should().Equal("Anna", "Boris");
        result.Items[0].DayOfNameDay.Should().Be(new DateOnly(2000, 7, 26));
        result.Items[0].ImageUrl.Should().Be("StaticFiles/Images/a.png");
        result.MetaData.Should().BeEquivalentTo(metaData);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetPersons_NullSearchTerm_IsSentAsEmpty()
    {
        _http.Expect(HttpMethod.Get, Api + "persons")
            .WithQueryString("searchTerm", "")
            .Respond(_ => PersonsPage(new MetaData()));

        var result = await _sut.GetPersons(new PersonParameters(), Xunit.TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetPersons_WithoutPaginationHeader_DescribesASinglePage()
    {
        _http.When(HttpMethod.Get, Api + "persons")
            .Respond(_ => PersonsPage(null, new { id = Guid.NewGuid(), name = "Anna", dayOfNameDay = "2000-07-26" }));

        var result = await _sut.GetPersons(new PersonParameters { PageNumber = 1, PageSize = 10 }, Xunit.TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.MetaData.HasNext.Should().BeFalse();
        result.MetaData.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task GetPersons_Failure_ThrowsApiExceptionWithServerMessage()
    {
        _http.When(HttpMethod.Get, Api + "persons").Respond(HttpStatusCode.BadRequest, "text/plain", "Object is null");

        var exception = await Record.ExceptionAsync(() => _sut.GetPersons(new PersonParameters(), Xunit.TestContext.Current.CancellationToken));

        exception.Should().BeOfType<ApiException>().Which.Message.Should().Be("Object is null");
    }

    // Review finding: the Index page only looked at the first 50 persons (the API's maximum page size).
    [Fact]
    public async Task GetAllPersons_ReadsEveryPage()
    {
        _http.Expect(HttpMethod.Get, Api + "persons").WithQueryString("pageNumber", "1").WithQueryString("pageSize", "50")
            .Respond(_ => PersonsPage(new MetaData { CurrentPage = 1, PageSize = 50, TotalPages = 2, TotalCount = 51 },
                new { id = Guid.NewGuid(), name = "Anna", dayOfNameDay = "2000-07-26" }));
        _http.Expect(HttpMethod.Get, Api + "persons").WithQueryString("pageNumber", "2").WithQueryString("pageSize", "50")
            .Respond(_ => PersonsPage(new MetaData { CurrentPage = 2, PageSize = 50, TotalPages = 2, TotalCount = 51 },
                new { id = Guid.NewGuid(), name = "Boris", dayOfNameDay = "2000-05-02" }));

        var persons = await _sut.GetAllPersons(Xunit.TestContext.Current.CancellationToken);

        persons.Select(p => p.Name).Should().Equal("Anna", "Boris");
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetPerson_RequestsById()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"persons/{id}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new { id, name = "Anna", dayOfNameDay = "2000-07-26" }));

        var person = await _sut.GetPerson(id, Xunit.TestContext.Current.CancellationToken);

        person!.Id.Should().Be(id);
        person.Name.Should().Be("Anna");
    }

    [Fact]
    public async Task GetPerson_NotFound_IsNull()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"persons/{id}").Respond(HttpStatusCode.NotFound);

        (await _sut.GetPerson(id, Xunit.TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetPerson_OtherFailure_ThrowsApiException()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"persons/{id}").Respond(HttpStatusCode.InternalServerError);

        var exception = await Record.ExceptionAsync(() => _sut.GetPerson(id, Xunit.TestContext.Current.CancellationToken));

        exception.Should().BeOfType<ApiException>().Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreatePerson_PostsDto()
    {
        _http.Expect(HttpMethod.Post, Api + "persons")
            .WithJsonContent<PersonForCreationDto>(p => p.Name == "Anna", TestJson.Web)
            .Respond(HttpStatusCode.Created);

        await _sut.CreatePerson(new PersonForCreationDto { Name = "Anna" });

        _http.VerifyNoOutstandingExpectation();
    }

    // Review finding: Create, Update and Delete discarded the response, so the pages reported success regardless.
    public static TheoryData<Func<PersonHttpRepository, Task>> Changes => new()
    {
        repository => repository.CreatePerson(new PersonForCreationDto { Name = "Anna" }),
        repository => repository.UpdatePerson(Guid.Empty, new PersonForUpdateDto { Name = "Anna" }),
        repository => repository.DeletePerson(Guid.Empty)
    };

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task Changes_Failure_ThrowApiExceptionWithServerMessage(Func<PersonHttpRepository, Task> change)
    {
        _http.When(Api + "persons*").Respond(HttpStatusCode.BadRequest, "text/plain", "Person does not exist");

        var exception = await Record.ExceptionAsync(() => change(_sut));

        var apiException = exception.Should().BeOfType<ApiException>().Subject;
        apiException.Message.Should().Be("Person does not exist");
        apiException.Handled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePerson_PutsToPersonUrl()
    {
        var id = Guid.NewGuid();
        _http.Expect(HttpMethod.Put, Api + $"persons/{id}")
            .WithJsonContent<PersonForUpdateDto>(p => p.Name == "New", TestJson.Web)
            .Respond(HttpStatusCode.NoContent);

        await _sut.UpdatePerson(id, new PersonForUpdateDto { Name = "New" });

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeletePerson_DeletesPersonUrl()
    {
        var id = Guid.NewGuid();
        _http.Expect(HttpMethod.Delete, Api + $"persons/{id}").Respond(HttpStatusCode.NoContent);

        await _sut.DeletePerson(id);

        _http.VerifyNoOutstandingExpectation();
    }

    // Review finding: the client stored BaseAddress + path, which breaks when the host changes.
    [Theory]
    [InlineData("StaticFiles/Images/anna.png")]
    [InlineData("\"StaticFiles/Images/anna.png\"")]
    public async Task UploadPersonImage_ReturnsThePathTheServerStoredTheImageUnder(string body)
    {
        _http.When(HttpMethod.Post, Api + "upload").Respond("text/plain", body);

        var url = await _sut.UploadPersonImage(new MultipartFormDataContent());

        url.Should().Be("StaticFiles/Images/anna.png");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Only image files can be uploaded.", "Only image files can be uploaded.")]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "", "The file is too large.")]
    public async Task UploadPersonImage_Failure_ThrowsApiExceptionWithMessage(HttpStatusCode status, string body,
        string expected)
    {
        _http.When(HttpMethod.Post, Api + "upload").Respond(status, "text/plain", body);

        var exception = await Record.ExceptionAsync(() => _sut.UploadPersonImage(new MultipartFormDataContent()));

        exception.Should().BeOfType<ApiException>().Which.Message.Should().Be(expected);
    }
}
