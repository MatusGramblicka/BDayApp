using BDayClient.AuthProvider;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.LocalStorage;
using Blazored.Toast;
using Blazored.Toast.Services;
using Entities.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BDayClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        ConfigureServices(builder.Services, builder.Configuration, builder.HostEnvironment.BaseAddress);

        await builder.Build().RunAsync();
    }

    /// <summary>The app's services (separate from <see cref="Main"/> so that tests can check the registrations).</summary>
    /// <param name="hostBaseAddress">The address the app was loaded from.</param>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration,
        string hostBaseAddress)
    {
        services.Configure<ApiConfiguration>(configuration.GetSection("ApiConfiguration"));

        // The client is hosted by BDayServer, so without an explicit ApiConfiguration:BaseAddress
        // (appsettings*.json are not in git) the API lives on the origin the app was loaded from.
        services.PostConfigure<ApiConfiguration>(apiConfiguration =>
        {
            if (string.IsNullOrWhiteSpace(apiConfiguration.BaseAddress))
                apiConfiguration.BaseAddress = hostBaseAddress;
        });

        services.AddSingleton<ApiUrls>();

        // Everything the HTTP message handlers below use is a singleton: IHttpClientFactory creates the handlers in a
        // DI scope of their own, where a scoped service would be a second, disconnected instance (a toast nobody
        // displays, a logout the UI never hears about). In WebAssembly, scoped services live as long as singletons anyway.
        services.AddBlazoredLocalStorageAsSingleton();
        services.AddBlazoredToast();
        services.Replace(ServiceDescriptor.Singleton<IToastService, ToastService>()); // registered as scoped above
        services.AddSingleton<TokenStorage>();
        services.AddSingleton<AuthStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<RefreshTokenService>();

        services.AddAuthorizationCore();

        // Token refresh and error handling for every API call, registered once here rather than in each page.
        services.AddTransient<ApiErrorHandler>();
        services.AddTransient<AuthTokenHandler>();
        services
            .AddHttpClient(ApiUrls.HttpClientName,
                (sp, client) => client.BaseAddress = sp.GetRequiredService<ApiUrls>().ApiBase)
            .AddHttpMessageHandler<ApiErrorHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddScoped(
            sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(ApiUrls.HttpClientName));

        services.AddScoped<IPersonHttpRepository, PersonHttpRepository>();
        services.AddScoped<IEventHttpRepository, EventHttpRepository>();
        services.AddScoped<IUsersHttpRepository, UsersHttpRepository>();
    }
}
