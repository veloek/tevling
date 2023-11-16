using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContextFactory<DataContext>(optionsBuilder =>
{
    string dbPath = Path.Join(Environment.CurrentDirectory, "storage", "spur.db");
    optionsBuilder.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAthleteService, AthleteService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();

builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

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

app.MapControllers();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using IServiceScope serviceScope = app.Services.CreateScope();
IDbContextFactory<DataContext> dataContextFactory =
    serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
using DataContext dataContext = await dataContextFactory.CreateDbContextAsync();
await dataContext.InitAsync();

await app.RunAsync();
