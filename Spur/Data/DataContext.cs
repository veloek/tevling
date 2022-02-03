using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class DataContext : DbContext, IDataContext
{
    public DbSet<Activity> Activities { get; set; }
    IQueryable<Activity> IDataContext.Activities => Activities;
    public DbSet<Athlete> Athletes { get; set; }
    IQueryable<Athlete> IDataContext.Athletes => Athletes;

    public string DbPath { get; }

#pragma warning disable CS8618
    public DataContext()
    {
        // var folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        var path = Environment.CurrentDirectory;
        DbPath = Path.Join(path, "spur.db");
    }
#pragma warning restore CS8618

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>()
            .HasOne(a => a.Athlete)
            .WithMany(a => a.Activities);

        modelBuilder.Entity<Athlete>()
            .HasMany(a => a.Challenges)
            .WithMany(c => c.Athletes);
    }

    public Task Init()
    {
        //Database.EnsureCreated();
        return Database.MigrateAsync();
    }

    public async Task<Athlete> AddAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        var entry = await Athletes.AddAsync(athlete, ct);
        await SaveChangesAsync(ct);
        return entry.Entity;
    }
}
