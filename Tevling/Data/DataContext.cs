using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tevling.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<Activity> _activities { private get; set; }
    public IQueryable<Activity> Activities => _activities.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<Athlete> _athletes { private get; set; }
    public IQueryable<Athlete> Athletes => _athletes.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<Challenge> _challenges { private get; set; }
    public IQueryable<Challenge> Challenges => _challenges.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<Following> _following { private get; set; }
    public IQueryable<Following> Following => _following.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<FollowRequest> _followRequests { private get; set; }
    public IQueryable<FollowRequest> FollowRequests => _followRequests.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<ChallengeGroup> _challengeGroups { private get; set; }
    public IQueryable<ChallengeGroup> ChallengeGroups => _challengeGroups.AsQueryable();

    /// <value>INTERNAL: Public setter only for EF Core</value>
    public DbSet<ChallengeTemplate> _challengeTemplates { private get; set; }
    public IQueryable<ChallengeTemplate> ChallengeTemplates => _challengeTemplates.AsQueryable();

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>().ToTable("Activities");
        modelBuilder.Entity<Athlete>().ToTable("Athletes");
        modelBuilder.Entity<Challenge>().ToTable("Challenges");
        modelBuilder.Entity<Following>().ToTable("Following");
        modelBuilder.Entity<FollowRequest>().ToTable("FollowRequests");
        modelBuilder.Entity<ChallengeGroup>().ToTable("ChallengeGroups");
        modelBuilder.Entity<ChallengeTemplate>().ToTable("ChallengeTemplates");

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
            .HasMany(a => a.ChallengeTemplates)
            .WithOne(c => c.CreatedBy);

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

        modelBuilder.Entity<ChallengeTemplate>()
            .HasOne(ct => ct.CreatedBy);

        modelBuilder.Entity<ChallengeTemplate>()
            .Property(ct => ct.Created)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<ChallengeGroup>()
            .HasOne<Athlete>()
            .WithMany()
            .HasForeignKey(g => g.CreatedById)
            .IsRequired();

        modelBuilder.Entity<ChallengeGroup>()
            .Property(ct => ct.Created)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<ChallengeGroup>()
            .HasMany(g => g.Members)
            .WithMany();
    }

    public Task InitAsync()
    {
        //Database.EnsureCreatedAsync();
        return Database.MigrateAsync();
    }

    public async Task<Athlete> AddAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = await _athletes.AddAsync(athlete, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> UpdateAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = _athletes.Update(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> RemoveAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = _athletes.Remove(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = await _activities.AddAsync(activity, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> UpdateActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = _activities.Update(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> RemoveActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = _activities.Remove(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> AddChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = await _challenges.AddAsync(challenge, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeTemplate> AddChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = await _challengeTemplates.AddAsync(challengeTemplate, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> UpdateChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = _challenges.Update(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> RemoveChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = _challenges.Remove(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeTemplate> RemoveChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = _challengeTemplates.Remove(challengeTemplate);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> AddFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = await _following.AddAsync(following, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    public async Task<FollowRequest> AddFollowerRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = await _followRequests.AddAsync(followRequest, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> RemoveFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = _following.Remove(following);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<FollowRequest> RemoveFollowRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = _followRequests.Remove(followRequest);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeGroup> AddChallengeGroupAsync(ChallengeGroup challengeGroup, CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = await _challengeGroups.AddAsync(challengeGroup, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    public async Task<ChallengeGroup> RemoveChallengeGroupAsync(ChallengeGroup challengeGroup,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = _challengeGroups.Remove(challengeGroup);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
}
