using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class Login
{
    private UserForAuthentication _userForAuthentication = new();

    [Inject] public IAuthenticationService AuthenticationService { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; }

    private bool ShowAuthError { get; set; }
    private string Error { get; set; }

    public async Task ExecuteLogin()
    {
        ShowAuthError = false;

        var result = await AuthenticationService.Login(_userForAuthentication);
        if (result.Is2StepVerificationRequired)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["provider"] = result.Provider,
                ["email"] = _userForAuthentication.Email
            };

            NavigationManager.NavigateTo(QueryHelpers.AddQueryString(
                "/twostepverification", queryParams));
        }

        if (!result.IsAuthSuccessful)
        {
            Error = result.ErrorMessage;
            ShowAuthError = true;
        }
        else
            NavigationManager.NavigateTo("/");
    }
}