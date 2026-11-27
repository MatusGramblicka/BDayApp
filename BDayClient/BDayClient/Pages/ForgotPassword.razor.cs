using BDayClient.Http;
using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class ForgotPassword
{
    private BDayClient.Pocos.ForgotPassword _forgotPass = new ();
    private bool _showSuccess;
    private IReadOnlyList<string> _errors = [];

    [Inject] public IAuthenticationService AuthService { get; set; } = default!;

    private async Task Submit()
    {
        _showSuccess = false;
        _errors = [];

        try
        {
            // Any 2xx is success (the API answers 204 - also for unknown addresses, so it reveals nothing).
            var result = await AuthService.ForgotPassword(_forgotPass);
            if (result.Succeeded)
                _showSuccess = true;
            else
                _errors = result.Errors;
        }
        catch (ApiException ex)
        {
            if (!ex.Handled)
                _errors = ex.Errors;
        }
    }
}
