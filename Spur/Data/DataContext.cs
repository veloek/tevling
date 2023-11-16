using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spur.Model;

namespace Spur.Data;

public class DataContext : DbContext
{
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Athlete> Athletes { get; set; }
    public DbSet<Challenge> Challenges { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
