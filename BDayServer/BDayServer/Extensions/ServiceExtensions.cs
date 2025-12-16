using BDayServer.ActionFilters;
using Core;
using Core.Managers;
using Core.Services;
using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Repository;
using System.Text;
using Core.Managers.ManagerInterfaces;
using Core.Services.Interfaces;
using EmailService.EmailService;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
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
                                        errorNumbersToAdd: null)
                    ))
            .AddDbContext<RepositoryContextScheduleJob>(opts =>
                opts.UseAzureSql(configuration.GetConnectionString("AzureSql"),
                    options => options.MigrationsAssembly("BDayServer")
                                      .EnableRetryOnFailure(
                                        maxRetryCount: 5,
                                        maxRetryDelay: TimeSpan.FromSeconds(30),
                                        errorNumbersToAdd: null)
                    ))
            .AddDbContext<PostgreDbRepositoryContext>(opts =>
                    opts.UseNpgsql(configuration.GetConnectionString("PostgresqlConnection"),
                        options => options.MigrationsAssembly("BDayServer")
                            .EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorCodesToAdd: null)
             ));

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
    public static void RegisterAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JWTSettings");
        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings["validIssuer"],
                ValidAudience = jwtSettings["validAudience"],
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(jwtSettings["securityKey"] ?? throw new InvalidOperationException()))
            };
        });
    }

    public static void RegisterAuthenticatedSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo { Title = "BDayServer", Version = "v1" });
            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            option.AddSecurityRequirement((document) => new OpenApiSecurityRequirement()
            { 
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [] 
            });
        });
    }
}