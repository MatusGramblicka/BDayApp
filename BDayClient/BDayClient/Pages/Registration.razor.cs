using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Registration
{
    //private UserForRegistrationDto _userForRegistration = new();
    private UserForRegistration _userForRegistration = new();
    
    [Inject] public IAuthenticationService AuthenticationService { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; }

    public bool ShowRegistrationErrors { get; set; }
    public IEnumerable<string> Errors { get; set; }

    public async Task Register()
    {
        ShowRegistrationErrors = false;

        var result = await AuthenticationService.RegisterUser(_userForRegistration);
        if (!result.IsSuccessfulRegistration)
        {
            Errors = result.Errors;
            ShowRegistrationErrors = true;
        }
        else
        {
            NavigationManager.NavigateTo("/login");
        }
    }
}