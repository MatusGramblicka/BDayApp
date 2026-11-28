using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class Login
{
    private UserForAuthentication _userForAuthentication = new();

    [Inject] public IAuthenticationService AuthenticationService { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private bool ShowAuthError { get; set; }
    private string? Error { get; set; }

    // Where to continue after logging in (set by RedirectToLogin and ApiErrorHandler).
    private string? ReturnUrl
    {
        get
        {
            var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
            return query.TryGetValue("returnUrl", out var returnUrl) ? returnUrl.ToString() : null;
        }
    }

    public async Task ExecuteLogin()
    {
        ShowAuthError = false;

        AuthResponseDto result;
        try
        {
            result = await AuthenticationService.Login(_userForAuthentication);
        }
        catch (ApiException ex)
        {
            if (!ex.Handled)
                ShowError(ex.Message);
            return;
        }

        if (result.Is2StepVerificationRequired)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["provider"] = result.Provider,
                ["email"] = _userForAuthentication.Email,
                ["returnUrl"] = ReturnUrl
            };

            NavigationManager.NavigateTo(QueryHelpers.AddQueryString("/twostepverification", queryParams));
            return;
        }

        if (!result.IsAuthSuccessful)
        {
            ShowError(result.ErrorMessage);
            return;
        }

        NavigationManager.NavigateTo(LoginNavigation.GetSafeReturnUrl(ReturnUrl));
    }

    private void ShowError(string message)
    {
        Error = message;
        ShowAuthError = true;
    }
}
