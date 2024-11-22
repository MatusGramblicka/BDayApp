using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class ResetPassword
{
    private BDayClient.Pocos.ResetPassword _resetPassword = new();
    
    private bool _showError;
    private bool _showSuccess;
    private IEnumerable<string> _errors;

    [Inject] public IAuthenticationService AuthService { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    protected override void OnInitialized()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var queryStrings = QueryHelpers.ParseQuery(uri.Query);
        if (queryStrings.TryGetValue("email", out var email) &&
            queryStrings.TryGetValue("token", out var token))
        {
            _resetPassword.Email = email;
            _resetPassword.Token = token;
        }
        else
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task Submit()
    {
        _showSuccess = _showError = false;
        var result = await AuthService.ResetPassword(_resetPassword);
        if (result.IsResetPasswordSuccessful)
            _showSuccess = true;
        else
        {
            _errors = result.Errors;
            _showError = true;
        }
    }
}