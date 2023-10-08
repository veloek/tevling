using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.FeatureManagement;
using Spur.Clients;
using Spur.Data;
using Spur.Services;

namespace Spur;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#pragma warning disable IDE0058 // Remove unnecessary expression value

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddControllers();
        builder.Services.AddFeatureManagement();

        IConfigurationSection section = builder.Configuration.GetSection(nameof(StravaConfig));
        StravaConfig stravaConfig = section.Get<StravaConfig>() ?? new();
        builder.Services.AddSingleton(stravaConfig);

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        builder.Services.AddDbContext<IDataContext, DataContext>()
            .AddScoped<IAthleteRepository, AthleteRepository>()
            .AddScoped<IActivityRepository, ActivityRepository>()
            .AddScoped<IChallengeRepository, ChallengeRepository>();

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
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

#pragma warning restore IDE0058 // Remove unnecessary expression value

        using IServiceScope serviceScope = app.Services.CreateScope();
        IDataContext dataContext = serviceScope.ServiceProvider.GetRequiredService<IDataContext>();
        await dataContext.InitAsync();

        await app.RunAsync();
    }
}
