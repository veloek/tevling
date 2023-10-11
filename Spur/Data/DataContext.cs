using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spur.Model;

namespace Spur.Data;

public class DataContext : DbContext, IDataContext
{
    public DbSet<Activity> Activities { get; set; }
    IQueryable<Activity> IDataContext.Activities => Activities;
    public DbSet<Athlete> Athletes { get; set; }
    IQueryable<Athlete> IDataContext.Athletes => Athletes;
    public DbSet<Challenge> Challenges { get; set; }
    IQueryable<Challenge> IDataContext.Challenges => Challenges;

    public string DbPath { get; }

#pragma warning disable CS8618
    public DataContext()
    {
        // var folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        string path = Environment.CurrentDirectory;
        DbPath = Path.Join(path, "storage", "spur.db");
    }
#pragma warning restore CS8618

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
#pragma warning disable IDE0058 // Remove unnecessary expression value
        modelBuilder.Entity<Activity>()
            .HasOne(a => a.Athlete)
            .WithMany(a => a.Activities);

        modelBuilder.Entity<Activity>()
            .OwnsOne(a => a.Details)
            .Property(d => d.StartDate)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<Athlete>()
            .HasMany(a => a.Challenges)
            .WithMany(c => c.Athletes);

        modelBuilder.Entity<Athlete>()
            .HasMany(a => a.Following)
            .WithMany(a => a.Followers)
            .UsingEntity<Following>(
                e => e.HasOne<Athlete>().WithMany().HasForeignKey(e => e.FolloweeId),
                e => e.HasOne<Athlete>().WithMany().HasForeignKey(e => e.FollowerId));
#pragma warning restore IDE0058 // Remove unnecessary expression value
    }

    public Task InitAsync()
    {
        //Database.EnsureCreatedAsync();
        return Database.MigrateAsync();
    }

    public async Task<Athlete> AddAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = await Athletes.AddAsync(athlete, ct);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<Athlete> UpdateAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Athletes.Update(athlete);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<Athlete> RemoveAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Athletes.Remove(athlete);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = await Activities.AddAsync(activity, ct);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<Activity> UpdateActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Activities.Update(activity);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<Activity> RemoveActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Activities.Remove(activity);
        _ = await SaveChangesAsync(ct);
        return entry.Entity;
    }
}
