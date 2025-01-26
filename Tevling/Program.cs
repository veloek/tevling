using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.FeatureManagement;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo
    .Console()
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
        .ReadFrom
        .Configuration(context.Configuration)
        .ReadFrom
        .Services(services)
        .Enrich
        .FromLogContext());

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddFeatureManagement();
builder.Services.AddHealthChecks();
builder.Services.AddLocalization();

IConfigurationSection section = builder.Configuration.GetSection(nameof(StravaConfig));
StravaConfig stravaConfig = section.Get<StravaConfig>() ?? new StravaConfig();
builder.Services.AddSingleton(stravaConfig);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(
        options =>
        {
            options.LoginPath = "/login";
            options.ReturnUrlParameter = "returnUrl";
        });

// Make sure our data directoy used to store the SQLite DB file exists.
string dataDir = Path.Join(Environment.CurrentDirectory, "storage");
Directory.CreateDirectory(dataDir);

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
    .PersistKeysToFileSystem(new DirectoryInfo(dataDir));

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<IAthleteService, AthleteService>();
builder.Services.AddSingleton<IChallengeService, ChallengeService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IBrowserTime, BrowserTime>();

builder.Services.AddStravaClient();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization(
    new RequestLocalizationOptions()
        .AddSupportedCultures(new[] { "en", "no", "nb", "nn" })
        .AddSupportedUICultures(new[] { "en", "no", "nb", "nn" })
        .SetDefaultCulture("en"));

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.UseSerilogRequestLogging();

app.MapHealthChecks("healthz");
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

try
{
    await using (AsyncServiceScope serviceScope = app.Services.CreateAsyncScope())
    {
        IDbContextFactory<DataContext> dataContextFactory =
            serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
        await InitDb(dataContextFactory);

        if (args.FirstOrDefault() is "import")
        {
            IAthleteService athleteService = serviceScope.ServiceProvider.GetRequiredService<IAthleteService>();
            IActivityService activityService = serviceScope.ServiceProvider.GetRequiredService<IActivityService>();

            await BatchImport(athleteService, activityService, args.ElementAtOrDefault(1));
            return;
        }
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

return;

static async Task InitDb(IDbContextFactory<DataContext> dataContextFactory)
{
    await using DataContext dataContext = await dataContextFactory.CreateDbContextAsync();
    await dataContext.Database.MigrateAsync();
}

static async Task BatchImport(IAthleteService athleteService, IActivityService activityService, string? timeSpan)
{
    TimeSpan maxSpan = TimeSpan.FromDays(7);
    TimeSpan span = TimeSpan.FromDays(1);

    if (timeSpan != null)
    {
        if (!TimeSpan.TryParse(timeSpan, out span))
            throw new ArgumentException("invalid timespan: " + timeSpan);

        if (span > maxSpan)
            throw new ArgumentException("timespan too long, max: " + maxSpan);
    }

    DateTimeOffset from = DateTimeOffset.Now - span;
    Athlete[] athletes = await athleteService.GetAthletesAsync();

    Log.Information("Batch importing activities for {AthleteCount} athletes from {From}", athletes.Length, from);

    foreach (Athlete athlete in athletes)
    {
        await activityService.ImportActivitiesForAthleteAsync(athlete.Id, from);
    }

    Log.Information("Batch import done");
}

// Workaround to make Program public for tests
public partial class Program { }
