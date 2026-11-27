using System.Net.Http.Json;
using BDayClient.AuthProvider;
using BDayClient.Http;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.HttpRepository;

/// <summary>
/// A singleton (see Program.cs): <see cref="AuthTokenHandler"/> and <see cref="ApiErrorHandler"/> use it too, and
/// IHttpClientFactory creates those in a DI scope of their own. That is also why it creates an HttpClient per call
/// instead of taking one in the constructor.
/// </summary>
public class AuthenticationService(
    IHttpClientFactory httpClientFactory,
    TokenStorage tokenStorage,
    AuthStateProvider authStateProvider,
    NavigationManager navigationManager) : IAuthenticationService
{
    private const string UnexpectedResponseMessage = "The server sent an unexpected response. Please try again.";

    private HttpClient Client => httpClientFactory.CreateClient(ApiUrls.HttpClientName);

    public async Task<AuthResponseDto> Login(UserForAuthentication userForAuthentication)
    {
        var userForAuthenticationDto = new UserForAuthenticationDto
        {
            Email = userForAuthentication.Email,
            Password = userForAuthentication.Password
        };

        using var response = await Client.PostAsJsonAsync("account/login", userForAuthenticationDto);
        return await CompleteLoginAsync(response);
    }

    public async Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto)
    {
        using var response = await Client.PostAsJsonAsync("account/twostepverification", twoFactorDto);
        return await CompleteLoginAsync(response);
    }

    public async Task Logout()
    {
        await tokenStorage.ClearAsync();
        authStateProvider.NotifyUserLogout();
    }

    public async Task<string> RefreshToken()
    {
        var token = await tokenStorage.GetAccessTokenAsync();
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();

        AuthResponseDto? result = null;
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))
        {
            using var response = await Client.PostAsJsonAsync("token/refresh",
                new RefreshTokenDto { Token = token, RefreshToken = refreshToken });

            if (response.IsSuccessStatusCode)
                result = await response.ReadJsonOrDefaultAsync<AuthResponseDto>();
        }

        // Refused (the refresh token rotates on every refresh and expires), or an answer we cannot use:
        // the session is over, so log out instead of carrying on with tokens that only produce 401s.
        if (result is not { IsAuthSuccessful: true } || string.IsNullOrEmpty(result.Token))
        {
            await Logout();
            return string.Empty;
        }

        await SignInAsync(result.Token, result.RefreshToken);
        return result.Token;
    }

    public async Task<ResponseDto> RegisterUser(UserForRegistration userForRegistration)
    {
        var userForRegistrationDto = new UserForRegistrationDto
        {
            Email = userForRegistration.Email,
            Password = userForRegistration.Password,
            ConfirmPassword = userForRegistration.ConfirmPassword,
            ClientUri = navigationManager.ToAbsoluteUri("emailconfirmation").ToString()
        };

        using var response = await Client.PostAsJsonAsync("account/register", userForRegistrationDto);
        if (response.IsSuccessStatusCode)
            return new ResponseDto { IsSuccessfulRegistration = true };

        var error = await ReadErrorAsync(response);
        return new ResponseDto { IsSuccessfulRegistration = false, Errors = error.Errors };
    }

    public async Task<ApiResult> ForgotPassword(ForgotPassword forgotPassword)
    {
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = forgotPassword.Email,
            ClientUri = navigationManager.ToAbsoluteUri("resetpassword").ToString()
        };

        using var response = await Client.PostAsJsonAsync("account/forgotpassword", forgotPasswordDto);
        return await ToApiResultAsync(response);
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

        using var response = await Client.PostAsJsonAsync("account/resetpassword", resetPasswordDto);
        if (response.IsSuccessStatusCode)
            return new ResetPasswordResponseDto { IsResetPasswordSuccessful = true };

        var error = await ReadErrorAsync(response);
        return new ResetPasswordResponseDto { IsResetPasswordSuccessful = false, Errors = error.Errors };
    }

    public async Task<ApiResult> EmailConfirmation(string email, string token)
    {
        var queryStringParam = new Dictionary<string, string?>
        {
            ["email"] = email,
            ["token"] = token
        };

        using var response = await Client.GetAsync(
            QueryHelpers.AddQueryString("account/emailconfirmation", queryStringParam));
        return await ToApiResultAsync(response);
    }

    private async Task<AuthResponseDto> CompleteLoginAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response);
            return Failed(error.Message);
        }

        var result = await response.ReadJsonOrDefaultAsync<AuthResponseDto>();
        if (result is null)
            return Failed(UnexpectedResponseMessage);

        if (result.Is2StepVerificationRequired)
            return result;

        if (string.IsNullOrEmpty(result.Token))
            return Failed(string.IsNullOrEmpty(result.ErrorMessage) ? UnexpectedResponseMessage : result.ErrorMessage);

        await SignInAsync(result.Token, result.RefreshToken);
        return new AuthResponseDto { IsAuthSuccessful = true };
    }

    private async Task SignInAsync(string token, string refreshToken)
    {
        await tokenStorage.StoreAsync(token, refreshToken);
        authStateProvider.NotifyUserAuthentication(token);
    }

    private static AuthResponseDto Failed(string errorMessage) =>
        new() { IsAuthSuccessful = false, ErrorMessage = errorMessage };

    private static async Task<ApiResult> ToApiResultAsync(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? ApiResult.Success()
            : ApiResult.Failure((await ReadErrorAsync(response)).Errors);

    private static async Task<ApiError> ReadErrorAsync(HttpResponseMessage response)
    {
        var error = await ApiErrorReader.ReadAsync(response);

        // Already shown by ApiErrorHandler (429, 5xx): end the flow without showing the same failure a second time.
        if (error.Handled)
            throw new ApiException(error);

        return error;
    }
}
