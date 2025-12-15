using BDayClient.AuthProvider;
using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Blazored.LocalStorage;
using Blazored.Toast;
using Entities.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace BDayClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        builder.Services.AddHttpClient("PersonsAPI", (sp, cl) =>
        {
            var apiConfiguration = sp.GetRequiredService<IOptions<ApiConfiguration>>();
            cl.BaseAddress =
                new Uri(apiConfiguration.Value.BaseAddress + "/api/");
            cl.EnableIntercept(sp);
        });

        builder.Services.AddBlazoredToast();

        builder.Services.AddScoped(
            sp => sp.GetService<IHttpClientFactory>().CreateClient("PersonsAPI"));

        builder.Services.AddHttpClientInterceptor();

        builder.Services.AddScoped<IPersonHttpRepository, PersonHttpRepository>();

        builder.Services.AddScoped<HttpInterceptorService>();

        builder.Services.Configure<ApiConfiguration>
            (builder.Configuration.GetSection("ApiConfiguration"));

        builder.Services.AddAutoMapper(a => a.AddProfile<MappingProfile>());

        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddAuthorizationCore();

        builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

        builder.Services.AddScoped<IUsersHttpRepository, UsersHttpRepository>();

        builder.Services.AddScoped<RefreshTokenService>();

        await builder.Build().RunAsync();
    }
}