using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tevling.Clients;
using Tevling.Data;

namespace Tevling.Integration;

public class TevlingWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string WhitelistedHost = "test.tld";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(ReplaceDbContext);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>()
            {
                [$"CultureByHost:{WhitelistedHost}"] = "en"
            });
        });
    }

    public WebApplicationFactory<Program> WithStravaClientHandler(Action<StravaClientHandler> configureHandler)
    {
        StravaClientHandler handler = new();
        configureHandler(handler);

        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddStravaClient()
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            });
        });
    }

    private static void ReplaceDbContext(IServiceCollection services)
    {
        ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType ==
                typeof(DbContextOptions<DataContext>));
        services.Remove(dbContextDescriptor!);

        ServiceDescriptor? dbConnectionDescriptor = services.SingleOrDefault(
            d => d.ServiceType ==
                typeof(DbConnection));
        services.Remove(dbConnectionDescriptor!);

        // Create open SqliteConnection so EF won't automatically close it.
        services.AddSingleton<DbConnection>(_ =>
        {
            SqliteConnection connection = new("DataSource=:memory:");
            connection.Open();

            return connection;
        });

        services.AddDbContextFactory<DataContext>((serviceProvider, options) =>
        {
            DbConnection connection = serviceProvider.GetRequiredService<DbConnection>();
            options.UseSqlite(connection);
        });
    }
}
