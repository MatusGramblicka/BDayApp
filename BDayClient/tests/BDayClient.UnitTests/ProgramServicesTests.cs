using System.Net;
using BDayClient.AuthProvider;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.LocalStorage;
using Blazored.Toast.Configuration;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests;

/// <summary>The registrations of Program.ConfigureServices, built the way the browser host builds them.</summary>
public class ProgramServicesTests
{
    private readonly MockHttpMessageHandler _server = new();
    private readonly LocalStorageMock _localStorage = new();
    private readonly TestNavigationManager _navigation = new();

    private ServiceProvider BuildProvider(Dictionary<string, string?>? settings = null)
    {
        var services = new ServiceCollection();

        // What WebAssemblyHostBuilder registers itself (singletons in the browser).
        services.AddSingleton<NavigationManager>(_navigation);
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddLogging();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        Program.ConfigureServices(services, configuration, TestNavigationManager.Base);

        // Test doubles for the browser: local storage and the network.
        services.AddSingleton(_localStorage.Object);
        services.AddHttpClient(ApiUrls.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _server);

        // The checks the host runs in Development, plus a check of every registration up front.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void AllServices_ResolveWithScopeValidation()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPersonHttpRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUsersHttpRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>().Should().BeOfType<AuthStateProvider>();
    }

    // IHttpClientFactory creates the message handlers in a DI scope of their own; what they share with the pages
    // (toasts, the authentication state, the running token refresh) must be the same instance there.
    [Fact]
    public void HandlerScope_SharesToastsAndAuthenticationWithTheApp()
    {
        using var provider = BuildProvider();
        using var appScope = provider.CreateScope();
        using var handlerScope = provider.CreateScope();

        handlerScope.ServiceProvider.GetRequiredService<IToastService>()
            .Should().BeSameAs(appScope.ServiceProvider.GetRequiredService<IToastService>());
        handlerScope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>()
            .Should().BeSameAs(appScope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>());
        handlerScope.ServiceProvider.GetRequiredService<RefreshTokenService>()
            .Should().BeSameAs(appScope.ServiceProvider.GetRequiredService<RefreshTokenService>());
        handlerScope.ServiceProvider.GetRequiredService<ILocalStorageService>()
            .Should().BeSameAs(appScope.ServiceProvider.GetRequiredService<ILocalStorageService>());
    }

    [Theory]
    [InlineData(null, "https://app.test/api/")]
    [InlineData("https://api.example.test", "https://api.example.test/api/")]
    [InlineData("https://api.example.test/", "https://api.example.test/api/")]
    public void ApiClient_BaseAddress(string? configured, string expected)
    {
        using var provider = BuildProvider(new() { ["ApiConfiguration:BaseAddress"] = configured });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<HttpClient>().BaseAddress!.ToString().Should().Be(expected);
    }

    [Fact]
    public async Task ApiClient_SendsTheToken_AndReportsFailuresCentrally()
    {
        var token = TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(30));
        _localStorage.StoreTokens(token);
        _server.Expect(HttpMethod.Get, "https://app.test/api/persons")
            .WithHeaders("Authorization", $"Bearer {token}")
            .Respond(HttpStatusCode.TooManyRequests);
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var response = await scope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync("persons", Xunit.TestContext.Current.CancellationToken);

        _server.VerifyNoOutstandingExpectation();
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ApiErrorReader.ReadAsync(response, Xunit.TestContext.Current.CancellationToken)).Handled.Should().BeTrue();
    }
}
