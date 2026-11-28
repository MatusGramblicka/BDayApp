using System.Text;
using System.Threading.RateLimiting;
using BDayServer.Security;
using Core.Managers;
using Core.Managers.ManagerInterfaces;
using Core.Services;
using Core.Services.Interfaces;
using EmailService.EmailService;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Repository;
using Repository.DatabaseAccessInterfaces;
using Repository.Repositories;
using Repository.Services;

namespace BDayServer.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("X-Pagination"));
        });

    public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddDbContext<DbRepositoryContext>(opts =>
                opts.UseAzureSql(configuration.GetConnectionString("AzureSql"),
                    options => options.MigrationsAssembly("BDayServer")
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)))
            .AddDbContext<PostgreDbRepositoryContext>(opts =>
                opts.UseNpgsql(configuration.GetConnectionString("PostgresqlConnection"),
                    options => options.MigrationsAssembly("BDayServer")
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null)));

    public static void ConfigureRepositoryManager(this IServiceCollection services) =>
        services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static void AddIdentityServices(this IServiceCollection services) =>
        services.AddIdentity<User, IdentityRole>(opt =>
            {
                opt.Lockout.AllowedForNewUsers = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
                opt.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddEntityFrameworkStores<DbRepositoryContext>()
            .AddDefaultTokenProviders();

    public static void RegisterEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailConfiguration>(configuration.GetSection(nameof(EmailConfiguration)));

        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IEmailPreparator, EmailPreparator>();
    }

    public static void RegisterAuthorizationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // GetUserProvider reads the signed-in user from the current request.
        services.AddHttpContextAccessor();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IGetUserProvider, GetUserProvider>();

        services.Configure<ClientAppOptions>(configuration.GetSection(ClientAppOptions.SectionName));
        services.AddSingleton<ClientUriValidator>();
    }

    public static void RegisterManagers(this IServiceCollection services)
    {
        services.AddScoped<IPersonManager, PersonManager>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<ITokenManager, TokenManager>();
        services.AddScoped<IUploadManager, UploadManager>();
        services.AddScoped<IAccountManager, AccountManager>();
        services.AddScoped<IPersonMirror, PostgresPersonMirror>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.TryAddSingleton(TimeProvider.System);
    }

    /// <summary>
    /// Where uploaded person images are stored (served under /StaticFiles). On Azure App Service this is
    /// %HOME%\data\StaticFiles: persistent storage outside the deployed app folder (%HOME%\site\wwwroot),
    /// so no deployment can overwrite or delete uploads. Locally it is the StaticFiles folder next to the app.
    /// The setting "Uploads:RootPath" overrides both.
    /// </summary>
    public static string ConfigureUploads(this IServiceCollection services, IConfiguration configuration)
    {
        var rootPath = configuration["Uploads:RootPath"];

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            var onAppService = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

            rootPath = onAppService && !string.IsNullOrEmpty(home)
                ? Path.Combine(home, "data", "StaticFiles")
                : Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles");
        }

        Directory.CreateDirectory(Path.Combine(rootPath, "Images"));
        services.Configure<UploadOptions>(o => o.RootPath = rootPath);

        return rootPath;
    }

    public static void RegisterAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Missing or weak JWT settings stop the app at startup instead of failing every login later.
        services.AddOptions<JwtConfiguration>()
            .Bind(configuration.GetSection(JwtConfiguration.SectionName))
            .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.ValidIssuer) && !string.IsNullOrWhiteSpace(jwt.ValidAudience),
                $"{JwtConfiguration.SectionName}: validIssuer and validAudience are required.")
            .Validate(jwt => Encoding.UTF8.GetByteCount(jwt.SecurityKey) >= 32,
                $"{JwtConfiguration.SectionName}: securityKey must be at least 32 bytes long (HMAC-SHA256).")
            .Validate(jwt => jwt.ExpiryInMinutes > 0,
                $"{JwtConfiguration.SectionName}: expiryInMinutes must be greater than 0.")
            .ValidateOnStart();

        services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Access tokens are validated with the same parameters that the token refresh uses (plus the lifetime check).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtConfiguration>>((options, jwt) =>
                options.TokenValidationParameters =
                    AuthenticationService.CreateTokenValidationParameters(jwt.Value, validateLifetime: true));
    }

    /// <summary>
    /// Per-client-IP limits for the endpoints that are anonymous, send e-mails or accept passwords and codes
    /// (brute force, e-mail flooding) and for uploads. Rejected requests get 429.
    /// </summary>
    public static void ConfigureRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Account, context => PerClientPerMinute(context, permitLimit: 10));
            options.AddPolicy(RateLimitPolicies.TokenRefresh, context => PerClientPerMinute(context, permitLimit: 30));
            options.AddPolicy(RateLimitPolicies.Upload, context => PerClientPerMinute(context, permitLimit: 20));
        });

    private static RateLimitPartition<string> PerClientPerMinute(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}
