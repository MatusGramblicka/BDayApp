using BDayClient.Features;
using BDayClient.Http;
using Contracts.DataTransferObjects.Event;
using Entities.RequestFeatures;

namespace BDayClient.HttpRepository;

/// <summary>Event calls. Every method throws an <see cref="ApiException"/> when the call fails.</summary>
public interface IEventHttpRepository
{
    Task<PagingResponse<EventDto>> GetEvents(EventParameters eventParameters,
        CancellationToken cancellationToken = default);

    /// <summary>The event, or null when there is no event with this id (404).</summary>
    Task<EventDto?> GetEvent(Guid id, CancellationToken cancellationToken = default);

    Task CreateEvent(EventForCreationDto eventDto);
    Task UpdateEvent(Guid id, EventForUpdateDto eventDto);
    Task DeleteEvent(Guid id);
}
