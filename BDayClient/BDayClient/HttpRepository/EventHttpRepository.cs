using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BDayClient.Features;
using BDayClient.Http;
using Contracts.DataTransferObjects.Event;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.HttpRepository;

public class EventHttpRepository(HttpClient client) : IEventHttpRepository
{
    public async Task<EventDto?> GetEvent(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"events/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSuccessAsync(cancellationToken);

        return await response.ReadJsonOrDefaultAsync<EventDto>(cancellationToken)
               ?? throw response.UnexpectedResponse();
    }

    public async Task<PagingResponse<EventDto>> GetEvents(EventParameters eventParameters,
        CancellationToken cancellationToken = default)
    {
        var queryStringParam = new Dictionary<string, string?>
        {
            ["pageNumber"] = eventParameters.PageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = eventParameters.PageSize.ToString(CultureInfo.InvariantCulture),
            ["searchTerm"] = eventParameters.SearchTerm ?? string.Empty,
            ["orderBy"] = eventParameters.OrderBy ?? string.Empty
        };

        using var response = await client.GetAsync(QueryHelpers.AddQueryString("events", queryStringParam),
            cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        var items = await response.ReadJsonOrDefaultAsync<List<EventDto>>(cancellationToken)
                    ?? throw response.UnexpectedResponse();

        return new PagingResponse<EventDto>
        {
            Items = items,
            MetaData = ReadMetaData(response) ?? new MetaData
            {
                CurrentPage = eventParameters.PageNumber,
                PageSize = eventParameters.PageSize,
                TotalCount = items.Count,
                TotalPages = eventParameters.PageNumber
            }
        };
    }

    public async Task CreateEvent(EventForCreationDto eventDto)
    {
        using var response = await client.PostAsJsonAsync("events", eventDto);
        await response.EnsureSuccessAsync();
    }

    public async Task UpdateEvent(Guid id, EventForUpdateDto eventDto)
    {
        using var response = await client.PutAsJsonAsync($"events/{id}", eventDto);
        await response.EnsureSuccessAsync();
    }

    public async Task DeleteEvent(Guid id)
    {
        using var response = await client.DeleteAsync($"events/{id}");
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
}
