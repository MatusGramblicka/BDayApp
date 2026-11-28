using BDayClient.Features;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class CreatePerson
{
    private PersonForCreation _person = NewPerson();

    [Inject] public IPersonHttpRepository PersonRepo { get; set; } = default!;

    [Inject] public IToastService ToastService { get; set; } = default!;

    // The nameday starts as today, in the year the day/month pickers store dates in.
    private static PersonForCreation NewPerson() => new()
    {
        DayOfNameDay = DayMonth.InStorageYear(DateOnly.FromDateTime(DateTime.Today))
    };

    private async Task Create()
    {
        try
        {
            await PersonRepo.CreatePerson(_person.ToDto());
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess($"Person \"{_person.Name}\" was added.");

        // A new model gives a new, untouched form.
        _person = NewPerson();
    }
}
