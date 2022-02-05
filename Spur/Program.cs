using Microsoft.AspNetCore.Authentication.Cookies;
using Spur.Data;
using Spur.Services;

namespace Spur;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddControllers();

        var section = builder.Configuration.GetSection(nameof(StravaConfig));
        var stravaConfig = section.Get<StravaConfig>();
        builder.Services.AddSingleton(stravaConfig);

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        builder.Services.AddDbContext<IDataContext, DataContext>()
            .AddScoped<IAthleteRepository, AthleteRepository>()
            .AddScoped<IActivityRepository, ActivityRepository>()
            .AddScoped<IChallengeRepository, ChallengeRepository>();

        builder.Services.AddScoped<IChallengeService, ChallengeService>();

        var app = builder.Build();

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

        using var serviceScope = app.Services.CreateScope();
        var dataContext = serviceScope.ServiceProvider.GetRequiredService<IDataContext>();
        await dataContext.InitAsync();

        await app.RunAsync();
    }
}
