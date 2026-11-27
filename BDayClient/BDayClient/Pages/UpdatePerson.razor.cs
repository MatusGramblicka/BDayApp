using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class UpdatePerson
{
    // Null while loading: the form is only rendered once there is a person to edit.
    private PersonForUpdate? _personForUpdate;
    private bool _loadFailed;
    private Guid? _loadedId;

    [Inject] public IPersonHttpRepository PersonRepo { get; set; } = default!;

    [Inject] public IToastService ToastService { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == Id)
            return;

        _loadedId = Id;
        _personForUpdate = null;
        _loadFailed = false;

        try
        {
            var person = await PersonRepo.GetPerson(Id);
            if (person is null)
            {
                NavigationManager.NavigateTo("/404");
                return;
            }

            _personForUpdate = PersonForUpdate.FromDto(person);
        }
        catch (ApiException ex)
        {
            _loadFailed = true;
            ToastService.ShowApiError(ex);
        }
    }

    private async Task Update()
    {
        if (_personForUpdate is null)
            return;

        var personForUpdateDto = _personForUpdate.ToDto();

        try
        {
            await PersonRepo.UpdatePerson(Id, personForUpdateDto);
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess($"Person \"{personForUpdateDto.Name}\" was updated.");

        // The saved values are the new starting point: the form counts as untouched until the next change.
        _personForUpdate = _personForUpdate.Copy();
    }
}
