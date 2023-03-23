using BDayClient.HttpRepository;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class Login
    {
        private UserForAuthenticationDto _userForAuthentication = new();

        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [Inject] public NavigationManager NavigationManager { get; set; }

        public bool ShowAuthError { get; set; }
        public string Error { get; set; }

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
            {
                NavigationManager.NavigateTo("/");
            }
        }
    }
}
