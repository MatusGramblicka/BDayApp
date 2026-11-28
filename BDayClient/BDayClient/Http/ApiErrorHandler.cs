using System.Net;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Http;

/// <summary>
/// Central handling of failed API calls, registered once on the API <see cref="HttpClient"/> (Program.cs):
/// <list type="bullet">
/// <item>401 from a call that is not an account/ or token/ endpoint: log out and go to the login page.</item>
/// <item>403: go to /unauthorized.</item>
/// <item>429 and 5xx: show a toast.</item>
/// <item>No response at all (server down, no network): show a toast and throw a handled <see cref="ApiException"/>.</item>
/// </list>
/// Other failures (400, 404, 409, 413, ...) are left to the caller, which turns them into an <see cref="ApiException"/>
/// with the server's message. Requests handled here are marked, so callers know the user has already been told
/// (<see cref="ApiError.Handled"/>).
/// </summary>
public sealed class ApiErrorHandler(
    ApiUrls apiUrls,
    NavigationManager navigationManager,
    IToastService toastService,
    IAuthenticationService authenticationService,
    ILogger<ApiErrorHandler> logger) : DelegatingHandler
{
    public const string ServerUnreachableMessage =
        "The server could not be reached. Please check your connection and try again.";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // In the browser every failed fetch (server down, no network, CORS) arrives as "TypeError: Failed to fetch".
            logger.LogWarning(ex, "{Method} {Path} failed without a response", request.Method,
                request.RequestUri?.AbsolutePath);
            toastService.ShowError(ServerUnreachableMessage);
            throw new ApiException(new ApiError(null, [ServerUnreachableMessage], Handled: true), ex);
        }

        if (response.IsSuccessStatusCode || !apiUrls.IsApiRequest(request.RequestUri))
            return response;

        response.RequestMessage ??= request;

        if (await TryHandleAsync(request, response, cancellationToken))
            ApiErrorReader.MarkHandled(request);

        return response;
    }

    private async Task<bool> TryHandleAsync(HttpRequestMessage request, HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        switch (response.StatusCode)
        {
            // account/ and token/ answer 401 for a wrong password or code: the login pages show that message themselves.
            case HttpStatusCode.Unauthorized when !apiUrls.IsAuthEndpoint(request.RequestUri):
                await authenticationService.Logout();
                navigationManager.NavigateToLogin();
                return true;

            case HttpStatusCode.Forbidden:
                navigationManager.NavigateTo("/unauthorized");
                return true;

            case HttpStatusCode.TooManyRequests:
                toastService.ShowError(ApiErrorReader.TooManyRequestsMessage);
                return true;

            case >= HttpStatusCode.InternalServerError:
                var error = await ApiErrorReader.ReadAsync(response, cancellationToken);
                logger.LogWarning("{Method} {Path} answered {StatusCode}", request.Method,
                    request.RequestUri?.AbsolutePath, (int)response.StatusCode);
                toastService.ShowError(error.Message);
                return true;

            default:
                return false;
        }
    }
}
