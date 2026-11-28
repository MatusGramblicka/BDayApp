using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Logout
{
    [Inject]
    public IAuthenticationService AuthenticationService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await AuthenticationService.Logout();
        NavigationManager.NavigateTo("/login");
    }
}
