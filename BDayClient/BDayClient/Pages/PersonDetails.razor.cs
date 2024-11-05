using BDayClient.HttpRepository;
using Entities.Models;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class PersonDetails
{
    private Person Person { get; set; } = new();

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; }

    [Parameter]
    public Guid PersonId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Person = await PersonRepo.GetPerson(PersonId);
    }
}