using BDayClient.HttpRepository;
using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class ForgotPassword
    {
        private ForgotPasswordDto _forgotPassDto = new();
        private bool _showSuccess;
        private bool _showError;

        [Inject] public IAuthenticationService AuthService { get; set; }

        private async Task Submit()
        {
            _showSuccess = _showError = false;

            var result = await AuthService.ForgotPassword(_forgotPassDto);
            if (result == HttpStatusCode.OK)
                _showSuccess = true;
            else
                _showError = true;
        }
    }
}
