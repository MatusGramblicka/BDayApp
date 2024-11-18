using BDayServer.ActionFilters;
using Contracts.EmailService;
using Core;
using Core.EmailService;
using Core.Managers;
using Core.Services;
using Entities;
using Entities.Configuration;
using Entities.Models;
using Interfaces;
using Interfaces.Authentication;
using Interfaces.DatabaseAccess;
using Interfaces.EmailService;
using Interfaces.Managers;
using Interfaces.UserProvider;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;

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
        services.AddDbContext<RepositoryContext>(opts =>
                opts.UseMySql(configuration.GetConnectionString("sqlConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("sqlConnection")),
                    b => b.MigrationsAssembly("BDayServer")))
            .AddDbContext<RepositoryContextScheduleJob>(opts =>
                opts.UseMySql(configuration.GetConnectionString("sqlConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("sqlConnection")),
                    b => b.MigrationsAssembly("BDayServer")));

    public static void ConfigureRepositoryManager(this IServiceCollection services) =>
        services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static void AddIdentityServices(this IServiceCollection services) =>
        services.AddIdentity<User, IdentityRole>(opt =>
            {
                opt.Lockout.AllowedForNewUsers = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
                opt.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddEntityFrameworkStores<RepositoryContext>()
            .AddDefaultTokenProviders();

    public static void RegisterActionFilters(this IServiceCollection services)
    {
        services.AddScoped<ValidationFilterAttribute>();
    }

    public static void RegisterEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailConfiguration>(configuration.GetSection(nameof(EmailConfiguration)));

        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IEmailPreparator, EmailPreparator>();
    }

    public static void RegisterAuthorizationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtConfiguration>(configuration.GetSection("JWTSettings"));

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IGetUserProvider, GetUserProvider>();
    }

    public static void RegisterMangers(this IServiceCollection services)
    {
        services.AddScoped<IPersonManager, PersonManager>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<ITokenManager, TokenManager>();
        services.AddScoped<IUploadManager, UploadManager>();
        services.AddScoped<ISwaggerLoginManager, SwaggerLoginManager>();
        services.AddScoped<IAccountManager, AccountManager>();
    }
}