﻿using BDayClient.HttpRepository;
using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class TwoStepVerification
    {
        private TwoFactorVerificationDto _twoFactorDto = new();
        private bool _showError;
        private string _error;

        [Inject] public IAuthenticationService AuthService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            var queryStrings = QueryHelpers.ParseQuery(uri.Query);
            if (queryStrings.TryGetValue("email", out var email) &&
                queryStrings.TryGetValue("provider", out var provider))
            {
                _twoFactorDto.Email = email;
                _twoFactorDto.Provider = provider;
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }

        private async Task Submit()
        {
            _showError = false;

            var result = await AuthService.LoginVerification(_twoFactorDto);
            if (result.IsAuthSuccessful)
                NavigationManager.NavigateTo("/");
            else
            {
                _error = result.ErrorMessage;
                _showError = true;
            }
        }
    }
}
