using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.FeatureManagement;
using Serilog;
using Serilog.Events;
using Tevling.Authentication;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Tevling...");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Extra (optional) config file from mounted configmap when running in k8s
string? appSettingsPath = builder.Configuration.GetValue<string>("TEVLING_APPSETTINGS");
if (!string.IsNullOrEmpty(appSettingsPath))
{
    Log.Information($"Adding optional config file with reloadOnChange: {appSettingsPath}");
    builder.Configuration.AddJsonFile(appSettingsPath, true, true);
}

builder.Host.UseSerilog(
    (context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddFeatureManagement();
builder.Services.AddHealthChecks();
builder.Services.AddLocalization();

builder.Services.Configure<StravaConfig>(builder.Configuration.GetSection(nameof(StravaConfig)));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(
        options =>
        {
            options.LoginPath = "/login";
            options.ReturnUrlParameter = "returnUrl";
        })
    .AddStravaAuthentication();

// Make sure our data directoy used to store the SQLite DB file exists.
string dataDir = Path.Join(Environment.CurrentDirectory, "storage");
DirectoryInfo dataDirInfo = Directory.CreateDirectory(dataDir);

builder.Services.AddDbContextFactory<DataContext>(
    optionsBuilder =>
    {
        string dbPath = Path.Join(dataDir, "tevling.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        optionsBuilder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
        // optionsBuilder.LogTo(Console.WriteLine);
    });

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(dataDirInfo);

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<IAthleteService, AthleteService>();
builder.Services.AddSingleton<IChallengeService, ChallengeService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IBrowserTime, BrowserTime>();

builder.Services.AddStravaClient();

builder.Services.AddSingleton<IDevService, DevService>();

builder.Services.AddHostedService<BatchImportService>();

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseRequestLocalization(
    new RequestLocalizationOptions()
        .AddSupportedCultures(new[] { "en", "no", "nb", "nn" })
        .AddSupportedUICultures(new[] { "en", "no", "nb", "nn" })
        .SetDefaultCulture("en"));

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHealthChecks("healthz");
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

try
{
    await using (AsyncServiceScope serviceScope = app.Services.CreateAsyncScope())
    {
        IDbContextFactory<DataContext> dataContextFactory =
            serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();

        await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync();
        await dataContext.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "An unhandled exception occurred");
}
finally
{
    Log.CloseAndFlush();
}

// Workaround to make Program public for tests
public partial class Program { }
