using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tevling.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Athlete> Athletes { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<Following> Following { get; set; }
    
    public DbSet<FollowRequest> FollowRequests { get; set; }

#pragma warning disable CS8618
#pragma warning restore CS8618

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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
        
        modelBuilder.Entity<Athlete>()
            .HasMany(a => a.PendingFollowing)
            .WithMany(a => a.PendingFollowers)
            .UsingEntity<FollowRequest>(
                e => e.HasOne<Athlete>().WithMany().HasForeignKey(e => e.FolloweeId),
                e => e.HasOne<Athlete>().WithMany().HasForeignKey(e => e.FollowerId));

        modelBuilder.Entity<Athlete>()
            .Property(a => a.Created)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<Challenge>()
            .HasOne(c => c.CreatedBy);

        modelBuilder.Entity<Challenge>()
            .HasOne(c => c.Winner);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Start)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<Challenge>()
            .Property(c => c.End)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<Challenge>()
            .HasMany(a => a.InvitedAthletes)
            .WithMany();
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
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> UpdateAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Athletes.Update(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> RemoveAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Athletes.Remove(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = await Activities.AddAsync(activity, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> UpdateActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Activities.Update(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> RemoveActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Activities.Remove(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> AddChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = await Challenges.AddAsync(challenge, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> UpdateChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = Challenges.Update(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> RemoveChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = Challenges.Remove(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> AddFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = await Following.AddAsync(following, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    public async Task<FollowRequest> AddFollowerRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = await FollowRequests.AddAsync(followRequest, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> RemoveFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = Following.Remove(following);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    
    public async Task<FollowRequest> RemoveFollowRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = FollowRequests.Remove(followRequest);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
}
