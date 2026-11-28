using BDayClient.AuthProvider;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.TestUtilities;

/// <summary>
/// The API HttpClient as Program.cs builds it (ApiErrorHandler, then AuthTokenHandler) on top of a mocked server,
/// with the real authentication services behind the handlers.
/// </summary>
internal sealed class TestApi
{
    public const string BaseAddress = "https://api.test";
    public const string Api = BaseAddress + "/api/";

    public TestApi()
    {
        Urls = new ApiUrls(BaseAddress);
        TokenStorage = new TokenStorage(LocalStorage.Object);
        AuthStateProvider = new AuthStateProvider(TokenStorage);
        HttpClientFactory = new StubHttpClientFactory(() => Client!); // the client is created below, before any call
        AuthService = new AuthenticationService(HttpClientFactory, TokenStorage, AuthStateProvider, Navigation);
        RefreshTokenService = new RefreshTokenService(TokenStorage, AuthService);

        var pipeline = new ApiErrorHandler(Urls, Navigation, Toasts.Object, AuthService,
            NullLogger<ApiErrorHandler>.Instance)
        {
            InnerHandler = new AuthTokenHandler(Urls, RefreshTokenService) { InnerHandler = Http }
        };

        Client = new HttpClient(pipeline) { BaseAddress = Urls.ApiBase };
    }

    // "Always": the When(...) answers (e.g. token/refresh) also apply while Expect(...) expectations are pending.
    public MockHttpMessageHandler Http { get; } = new(BackendDefinitionBehavior.Always);
    public LocalStorageMock LocalStorage { get; } = new();
    public TestNavigationManager Navigation { get; } = new();
    public Mock<IToastService> Toasts { get; } = new();
    public ApiUrls Urls { get; }
    public TokenStorage TokenStorage { get; }
    public AuthStateProvider AuthStateProvider { get; }
    public StubHttpClientFactory HttpClientFactory { get; }
    public AuthenticationService AuthService { get; }
    public RefreshTokenService RefreshTokenService { get; }
    public HttpClient Client { get; }

    public void VerifyErrorToast(string message, Times times) =>
        Toasts.Verify(t => t.ShowError(message, It.IsAny<Action<Blazored.Toast.Configuration.ToastSettings>?>()), times);

    public void VerifyNoErrorToast() =>
        Toasts.Verify(t => t.ShowError(It.IsAny<string>(), It.IsAny<Action<Blazored.Toast.Configuration.ToastSettings>?>()),
            Times.Never);
}
