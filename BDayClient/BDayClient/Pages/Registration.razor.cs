using BDayClient.HttpRepository;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class Registration
    {
        private UserForRegistrationDto _userForRegistration = new();

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
}
