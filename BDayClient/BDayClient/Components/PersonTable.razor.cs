using BDayClient.Http;
using BDayClient.Shared;
using Contracts.DataTransferObjects.Person;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class PersonTable
{
    /// <summary>The persons to list; null while they are loading.</summary>
    [Parameter]
    public IReadOnlyList<PersonDto>? Persons { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    private Confirmation? _confirmation;
    private Guid _personIdToDelete;

    private void CallConfirmationModal(Guid id)
    {
        _personIdToDelete = id;
        _confirmation?.Show();
    }

    private async Task DeletePerson()
    {
        _confirmation?.Hide();
        await OnDelete.InvokeAsync(_personIdToDelete);
    }
}
