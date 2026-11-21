using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BDayClient.HttpRepository;
using Contracts.DataTransferObjects.Person;
using Entities.Configuration;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.Extensions.Options;
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
        _sut = new PersonHttpRepository(client,
            Options.Create(new ApiConfiguration { BaseAddress = "https://api.test" }));
    }

    [Fact]
    public async Task GetPersons_SendsQueryParameters_AndParsesItemsAndPaginationHeader()
    {
        var metaData = new MetaData { CurrentPage = 2, PageSize = 4, TotalCount = 9, TotalPages = 3 };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[]
            {
                new { id = Guid.NewGuid(), name = "Anna", dayOfNameDay = "2000-07-26" },
                new { id = Guid.NewGuid(), name = "Boris", dayOfNameDay = "2000-05-02" }
            })
        };
        response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metaData));

        _http.Expect(HttpMethod.Get, Api + "persons")
            .WithExactQueryString(new Dictionary<string, string>
            {
                ["pageNumber"] = "2",
                ["pageSize"] = "4",
                ["searchTerm"] = "an",
                ["orderBy"] = "name desc"
            })
            .Respond(_ => response);

        var result = await _sut.GetPersons(new PersonParameters
        {
            PageNumber = 2, PageSize = 4, SearchTerm = "an", OrderBy = "name desc"
        });

        result.Items.Select(p => p.Name).Should().Equal("Anna", "Boris");
        result.Items[0].DayOfNameDay.Should().Be(new DateOnly(2000, 7, 26));
        result.MetaData.Should().BeEquivalentTo(metaData);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetPersons_NullSearchTerm_IsSentAsEmpty()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<Person>()) };
        response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new MetaData()));
        _http.Expect(HttpMethod.Get, Api + "persons")
            .WithQueryString("searchTerm", "")
            .Respond(_ => response);

        var result = await _sut.GetPersons(new PersonParameters());

        result.Items.Should().BeEmpty();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetPerson_RequestsById()
    {
        var id = Guid.NewGuid();
        _http.When(HttpMethod.Get, Api + $"persons/{id}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new { id, name = "Anna", dayOfNameDay = "2000-07-26" }));

        var person = await _sut.GetPerson(id);

        person.Id.Should().Be(id);
        person.Name.Should().Be("Anna");
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

    [Fact]
    public async Task UploadPersonImage_ReturnsUrlRelativeToApiBaseAddress()
    {
        _http.When(HttpMethod.Post, Api + "upload").Respond("text/plain", "StaticFiles/Images/anna.png");

        var url = await _sut.UploadPersonImage(new MultipartFormDataContent());

        url.Should().StartWith("https://api.test").And.EndWith("StaticFiles/Images/anna.png");
    }
}
