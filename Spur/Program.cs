using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.FeatureManagement;
using Spur;
using Spur.Clients;
using Spur.Data;
using Spur.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddFeatureManagement();

IConfigurationSection section = builder.Configuration.GetSection(nameof(StravaConfig));
StravaConfig stravaConfig = section.Get<StravaConfig>() ?? new();
builder.Services.AddSingleton(stravaConfig);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ReturnUrlParameter = "returnUrl";
    });

// Make sure our data directoy used to store the SQLite DB file exists.
string dataDir = Path.Join(Environment.CurrentDirectory, "storage");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContextFactory<DataContext>(optionsBuilder =>
{
    string dbPath = Path.Join(dataDir, "spur.db");
    optionsBuilder.UseSqlite($"Data Source={dbPath}");
    optionsBuilder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    // optionsBuilder.LogTo(Console.WriteLine);
});

builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<IAthleteService, AthleteService>();
builder.Services.AddSingleton<IChallengeService, ChallengeService>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddHttpClient<IStravaClient, StravaClient>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using IServiceScope serviceScope = app.Services.CreateScope();
IDbContextFactory<DataContext> dataContextFactory =
    serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
using DataContext dataContext = await dataContextFactory.CreateDbContextAsync();
await dataContext.InitAsync();

await app.RunAsync();
