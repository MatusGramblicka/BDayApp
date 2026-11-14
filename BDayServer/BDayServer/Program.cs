using BDayServer.Extensions;
using BDayServer.HostedService;
using BDayServer.Mapping;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using NLog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

LogManager.Setup().LoadConfigurationFromFile(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));

// ----------------------------
// Services (from ConfigureServices)
// ----------------------------

builder.Services.ConfigureCors();
builder.Services.ConfigureSqlContext(configuration);
builder.Services.ConfigureRepositoryManager();

builder.Services.AddAutoMapper(a => a.AddProfile<MappingProfile>());

builder.Services.RegisterActionFilters();
builder.Services.AddIdentityServices();
builder.Services.RegisterManagers();
builder.Services.RegisterAuthentication(configuration);
builder.Services.RegisterAuthorizationServices(configuration);
builder.Services.RegisterEmailServices(configuration);

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new OpenApiInfo
        {
            Title = "TravelOrders API",
            Version = "v1"
        };
        return Task.CompletedTask;
    });
});

// Cron job
builder.Services.AddCronJob<ScheduleJob>(c =>
{
    c.TimeZoneInfo = TimeZoneInfo.Utc;
    c.CronExpression = @"0 12 * * *";
});

var app = builder.Build();

// ----------------------------
// Middleware (from Configure)
// ----------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();

    app.MapOpenApi("/openapi/v1.json");
    app.MapScalarApiReference(); // UI at /scalar
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Additional static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles")),
    RequestPath = "/StaticFiles"
});

// Forwarded headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
