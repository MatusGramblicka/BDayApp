using BDayClient.Features;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class CreateEvent
{
    private EventForCreation _event = NewEvent();

    [Inject] public IEventHttpRepository EventRepo { get; set; } = default!;

    [Inject] public IToastService ToastService { get; set; } = default!;

    private static EventForCreation NewEvent() => new() { Date = DayMonth.InStorageYear(DateOnly.FromDateTime(DateTime.Today)) };

    private async Task Create()
    {
        try
        {
            await EventRepo.CreateEvent(_event.ToDto());
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess($"Event \"{_event.Name}\" was added.");

        // A new model gives a new, untouched form.
        _event = NewEvent();
    }
}
