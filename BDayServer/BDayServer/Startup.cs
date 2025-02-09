using BDayServer.Extensions;
using BDayServer.HostedService;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;

namespace BDayServer;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureCors();

        services.ConfigureSqlContext(Configuration);

        services.ConfigureRepositoryManager();

        services.AddAutoMapper(typeof(Startup));

        services.RegisterActionFilters();

        services.AddIdentityServices();

        services.RegisterMangers();

        services.RegisterAuthentication(Configuration);        

        services.RegisterAuthorizationServices(Configuration);
        
        services.RegisterEmailServices(Configuration);

        services.AddControllers();

        services.RegisterAuthenticatedSwagger();        

        services.AddRazorPages();

        services.AddCronJob<ScheduleJob>(c =>
        {
            c.TimeZoneInfo = TimeZoneInfo.Utc;
            c.CronExpression = @"0 12 * * *";               
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BDayServer v1"));
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors("CorsPolicy");

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(),
                @"StaticFiles")),
            RequestPath = new PathString("/StaticFiles")
        });

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All
        });

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapFallbackToFile("index.html");
        });
    }
}