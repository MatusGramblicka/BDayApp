using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace BDayClient.Pages;

public partial class ForgotPassword
{
    private BDayClient.Pocos.ForgotPassword _forgotPass = new ();
    private bool _showSuccess;
    private bool _showError;

    [Inject] public IAuthenticationService AuthService { get; set; }

    private async Task Submit()
    {
        _showSuccess = _showError = false;

        var result = await AuthService.ForgotPassword(_forgotPass);
        if (result == HttpStatusCode.OK)
            _showSuccess = true;
        else
            _showError = true;
    }
}