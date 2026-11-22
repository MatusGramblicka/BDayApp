using BDayClient.AuthProvider;
using BDayClient.Pocos;
using Blazored.LocalStorage;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BDayClient.HttpRepository;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _options = new() {PropertyNameCaseInsensitive = true};

    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navManager;

    public AuthenticationService(HttpClient client,
        AuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage,
        NavigationManager navManager)
    {
        _client = client;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _navManager = navManager;
    }

    public async Task<AuthResponseDto> Login(UserForAuthentication userForAuthentication)
    {
        var userForAuthenticationDto = new UserForAuthenticationDto
        {
            Email = userForAuthentication.Email,
            Password = userForAuthentication.Password
        };

        var response = await _client.PostAsJsonAsync("account/login",
            userForAuthenticationDto);
        var result = await ReadAuthResponseAsync(response);

        if (!response.IsSuccessStatusCode || result.Is2StepVerificationRequired || string.IsNullOrEmpty(result.Token))
            return result;

        await _localStorage.SetItemAsync("authToken", result.Token);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

        ((AuthStateProvider) _authStateProvider).NotifyUserAuthentication(
            result.Token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "bearer", result.Token);

        return new AuthResponseDto {IsAuthSuccessful = true};
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");

        ((AuthStateProvider) _authStateProvider).NotifyUserLogout();

        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<string> RefreshToken()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

        var response = await _client.PostAsJsonAsync("token/refresh",
            new RefreshTokenDto
            {
                Token = token,
                RefreshToken = refreshToken
            });

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponseDto>(content, _options);

        await _localStorage.SetItemAsync("authToken", result.Token);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue
            ("bearer", result.Token);

        return result.Token;
    }

    public async Task<ResponseDto> RegisterUser(UserForRegistration userForRegistration)
    {
        var userForRegistrationDto = new UserForRegistrationDto
        {
            Email = userForRegistration.Email,
            ClientUri = Path.Combine(
                _navManager.BaseUri, "emailconfirmation"),
            ConfirmPassword = userForRegistration.ConfirmPassword,
            Password = userForRegistration.Password,
        };

        //userForRegistrationDto.ClientUri = Path.Combine(
        //    _navManager.BaseUri, "emailconfirmation");

        var response = await _client.PostAsJsonAsync("account/register",
            userForRegistrationDto);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ResponseDto>(content, _options);

            return result;
        }

        return new ResponseDto {IsSuccessfulRegistration = true};
    }

    public async Task<HttpStatusCode> ForgotPassword(ForgotPassword forgotPassword)
    {
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = forgotPassword.Email,
            ClientUri = Path.Combine(_navManager.BaseUri, "resetpassword")
        };

        //forgotPasswordDto.ClientUri =
        //    Path.Combine(_navManager.BaseUri, "resetpassword");

        var result = await _client.PostAsJsonAsync("account/forgotpassword",
            forgotPasswordDto);

        return result.StatusCode;
    }

    public async Task<ResetPasswordResponseDto> ResetPassword(ResetPassword resetPassword)
    {
        var resetPasswordDto = new ResetPasswordDto
        {
            Email = resetPassword.Email,
            Password = resetPassword.Password,
            ConfirmPassword = resetPassword.ConfirmPassword,
            Token = resetPassword.Token
        };

        var resetResult = await _client.PostAsJsonAsync("account/resetpassword",
            resetPasswordDto);

        var resetContent = await resetResult.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ResetPasswordResponseDto>(resetContent,
            _options);

        return result;
    }

    public async Task<HttpStatusCode> EmailConfirmation(string email, string token)
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["email"] = email,
            ["token"] = token
        };

        var response = await _client.GetAsync(QueryHelpers.AddQueryString(
            "account/emailconfirmation", queryStringParam));

        return response.StatusCode;
    }

    public async Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto)
    {
        var response = await _client.PostAsJsonAsync("account/twostepverification",
            twoFactorDto);
        var result = await ReadAuthResponseAsync(response);

        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(result.Token))
            return result;

        await _localStorage.SetItemAsync("authToken", result.Token);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

        ((AuthStateProvider) _authStateProvider).NotifyUserAuthentication(
            result.Token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "bearer", result.Token);

        return new AuthResponseDto {IsAuthSuccessful = true};
    }

    // Rejected logins come back as an AuthResponseDto (401), but other failures do not: the API answers
    // unexpected errors with plain text ("Unspecified problem") and IIS with an HTML page (e.g. 503 while
    // the site restarts after a deploy). Turn those into a failed result instead of a JSON exception.
    private async Task<AuthResponseDto> ReadAuthResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var result = JsonSerializer.Deserialize<AuthResponseDto>(content, _options);
            if (result is not null)
                return result;
        }
        catch (JsonException)
        {
            // Not an AuthResponseDto - handled below.
        }

        var serverMessage = content.Trim();
        var isShortText = serverMessage.Length is > 0 and <= 200 && !serverMessage.StartsWith('<');

        return new AuthResponseDto
        {
            IsAuthSuccessful = false,
            ErrorMessage = isShortText
                ? $"Login failed: {serverMessage}"
                : $"Login failed: the server answered {(int)response.StatusCode} {response.ReasonPhrase}. Please try again in a moment."
        };
    }
}