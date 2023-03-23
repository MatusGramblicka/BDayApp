using Contracts;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;

namespace BDayServer.Extensions
{
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
    }
}
