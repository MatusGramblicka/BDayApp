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
var uploadsRoot = builder.Services.ConfigureUploads(configuration);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
// Unhandled errors are answered as RFC 7807 problem details (see UseExceptionHandler below).
builder.Services.AddProblemDetails();

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
    // Logs the exception and returns a 500 problem-details response. (It used to re-run the request as "/Error",
    // a page that does not exist, so API errors came back as index.html with status 200.)
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Uploaded person images, served under /StaticFiles. New uploads are stored in uploadsRoot (outside the deployed app
// on Azure, see ConfigureUploads); images uploaded earlier into the app's own StaticFiles folder are still served from there.
var legacyStaticFilesRoot = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles");
IFileProvider uploadsFileProvider = new PhysicalFileProvider(uploadsRoot);
if (Directory.Exists(legacyStaticFilesRoot) &&
    !string.Equals(Path.GetFullPath(uploadsRoot).TrimEnd('\\', '/'),
        Path.GetFullPath(legacyStaticFilesRoot).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
{
    uploadsFileProvider = new CompositeFileProvider(uploadsFileProvider, new PhysicalFileProvider(legacyStaticFilesRoot));
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = uploadsFileProvider,
    RequestPath = "/StaticFiles",
    // Browsers must never treat an uploaded file as anything other than its declared (image) type.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.XContentTypeOptions = "nosniff"
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
// Unknown /api/... URLs are real 404s; every other unknown URL is a client-side route of the Blazor app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
