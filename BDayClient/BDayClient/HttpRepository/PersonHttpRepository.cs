using BDayClient.Features;
using Entities.Configuration;
using Entities.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace BDayClient.HttpRepository
{
    public class PersonHttpRepository : IPersonHttpRepository
    {
        private readonly HttpClient _client;
        private readonly NavigationManager _navManager;
        private readonly ApiConfiguration _apiConfiguration;

        private readonly JsonSerializerOptions _options = new() {PropertyNameCaseInsensitive = true};

        public PersonHttpRepository(HttpClient client, NavigationManager navManager,
            IOptions<ApiConfiguration> configuration)
        {
            _client = client;
            _navManager = navManager;
            _apiConfiguration = configuration.Value;
        }

        public async Task<Person> GetPerson(Guid id)
        {
            var person = await _client.GetFromJsonAsync<Person>($"persons/{id}");

            return person;
        }

        public async Task<PagingResponse<Person>> GetPersons(PersonParameters personParameters)
        {
            var queryStringParam = new Dictionary<string, string>
            {
                ["pageNumber"] = personParameters.PageNumber.ToString(),
                ["pageSize"] = personParameters.PageSize.ToString(),
                ["searchTerm"] = personParameters.SearchTerm == null ? "" : personParameters.SearchTerm,
                ["orderBy"] = personParameters.OrderBy == null ? "" : personParameters.OrderBy
            };

            var response =
                await _client.GetAsync(QueryHelpers.AddQueryString("persons", queryStringParam));

            var content = await response.Content.ReadAsStringAsync();

            var pagingResponse = new PagingResponse<Person>
            {
                Items = JsonSerializer.Deserialize<List<Person>>(content, _options),
                MetaData = JsonSerializer.Deserialize<MetaData>(
                    response.Headers.GetValues("X-Pagination").First(), _options)
            };

            return pagingResponse;
        }

        public async Task CreatePerson(PersonForCreationDto person)
            => await _client.PostAsJsonAsync("persons", person);

        public async Task<string> UploadPersonImage(MultipartFormDataContent content)
        {
            var postResult = await _client.PostAsync("upload", content);
            var postContent = await postResult.Content.ReadAsStringAsync();
            var imgUrl =
                Path.Combine(_apiConfiguration.BaseAddress, postContent);

            return imgUrl;
        }

        public async Task UpdatePerson(Guid id, PersonForUpdateDto person)
            => await _client.PutAsJsonAsync(Path.Combine("persons",
                id.ToString()), person);

        public async Task DeletePerson(Guid id)
            => await _client.DeleteAsync(Path.Combine("persons", id.ToString()));
    }
}
