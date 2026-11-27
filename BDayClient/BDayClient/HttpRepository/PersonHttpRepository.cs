using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BDayClient.Features;
using BDayClient.Http;
using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.HttpRepository;

public class PersonHttpRepository(HttpClient client) : IPersonHttpRepository
{
    // The API's maximum page size.
    private const int MaxPageSize = 50;

    // Safety net for GetAllPersons, should the pagination header ever be wrong.
    private const int MaxPages = 200;

    public async Task<PersonDto?> GetPerson(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"persons/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSuccessAsync(cancellationToken);

        return await response.ReadJsonOrDefaultAsync<PersonDto>(cancellationToken)
               ?? throw response.UnexpectedResponse();
    }

    public async Task<PagingResponse<PersonDto>> GetPersons(PersonParameters personParameters,
        CancellationToken cancellationToken = default)
    {
        var queryStringParam = new Dictionary<string, string?>
        {
            ["pageNumber"] = personParameters.PageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = personParameters.PageSize.ToString(CultureInfo.InvariantCulture),
            ["searchTerm"] = personParameters.SearchTerm ?? string.Empty,
            ["orderBy"] = personParameters.OrderBy ?? string.Empty
        };

        using var response = await client.GetAsync(QueryHelpers.AddQueryString("persons", queryStringParam),
            cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        var items = await response.ReadJsonOrDefaultAsync<List<PersonDto>>(cancellationToken)
                    ?? throw response.UnexpectedResponse();

        return new PagingResponse<PersonDto>
        {
            Items = items,
            MetaData = ReadMetaData(response) ?? new MetaData
            {
                CurrentPage = personParameters.PageNumber,
                PageSize = personParameters.PageSize,
                TotalCount = items.Count,
                TotalPages = personParameters.PageNumber
            }
        };
    }

    public async Task<List<PersonDto>> GetAllPersons(CancellationToken cancellationToken = default)
    {
        var personParameters = new PersonParameters { PageSize = MaxPageSize };
        var persons = new List<PersonDto>();

        while (true)
        {
            var page = await GetPersons(personParameters, cancellationToken);
            persons.AddRange(page.Items);

            if (!page.MetaData.HasNext || page.Items.Count == 0 || personParameters.PageNumber >= MaxPages)
                return persons;

            personParameters.PageNumber++;
        }
    }

    public async Task CreatePerson(PersonForCreationDto person)
    {
        using var response = await client.PostAsJsonAsync("persons", person);
        await response.EnsureSuccessAsync();
    }

    public async Task<string> UploadPersonImage(MultipartFormDataContent content)
    {
        using var response = await client.PostAsync("upload", content);
        await response.EnsureSuccessAsync();

        var imagePath = ReadPlainText(await response.Content.ReadAsStringAsync());
        return string.IsNullOrEmpty(imagePath) ? throw response.UnexpectedResponse() : imagePath;
    }

    public async Task UpdatePerson(Guid id, PersonForUpdateDto person)
    {
        using var response = await client.PutAsJsonAsync($"persons/{id}", person);
        await response.EnsureSuccessAsync();
    }

    public async Task DeletePerson(Guid id)
    {
        using var response = await client.DeleteAsync($"persons/{id}");
        await response.EnsureSuccessAsync();
    }

    private static MetaData? ReadMetaData(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Pagination", out var values))
            return null;

        try
        {
            // The API writes the header with PascalCase names; the options are case-insensitive.
            return JsonSerializer.Deserialize<MetaData>(values.First(), HttpResponseMessageExtensions.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // The upload answers with the path as plain text; tolerate it arriving as a JSON string as well.
    private static string ReadPlainText(string content)
    {
        var text = content.Trim();
        if (!text.StartsWith('"'))
            return text;

        try
        {
            return JsonSerializer.Deserialize<string>(text)?.Trim() ?? string.Empty;
        }
        catch (JsonException)
        {
            return text;
        }
    }
}
