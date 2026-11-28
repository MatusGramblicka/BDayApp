using BDayClient.Http;
using BDayClient.Shared;
using Contracts.DataTransferObjects.Event;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class EventTable
{
    /// <summary>The events to list; null while they are loading.</summary>
    [Parameter]
    public IReadOnlyList<EventDto>? Events { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    private Confirmation? _confirmation;
    private Guid _eventIdToDelete;

    private void CallConfirmationModal(Guid id)
    {
        _eventIdToDelete = id;
        _confirmation?.Show();
    }

    private async Task DeleteEvent()
    {
        _confirmation?.Hide();
        await OnDelete.InvokeAsync(_eventIdToDelete);
    }
}
