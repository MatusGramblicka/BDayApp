﻿using BDayClient.HttpRepository;
using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class ResetPassword
    {
        private readonly ResetPasswordDto _resetPassDto = new();
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
                _resetPassDto.Email = email;
                _resetPassDto.Token = token;
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }

        private async Task Submit()
        {
            _showSuccess = _showError = false;
            var result = await AuthService.ResetPassword(_resetPassDto);
            if (result.IsResetPasswordSuccessful)
                _showSuccess = true;
            else
            {
                _errors = result.Errors;
                _showError = true;
            }
        }
    }
}
