using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tevling.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public IQueryable<Activity> Activities => Set<Activity>().AsQueryable();
    public IQueryable<Athlete> Athletes => Set<Athlete>().AsQueryable();
    public IQueryable<Challenge> Challenges => Set<Challenge>().AsQueryable();
    public IQueryable<Following> Following => Set<Following>().AsQueryable();
    public IQueryable<FollowRequest> FollowRequests => Set<FollowRequest>().AsQueryable();
    public IQueryable<ChallengeGroup> ChallengeGroups => Set<ChallengeGroup>().AsQueryable();
    public IQueryable<ChallengeTemplate> ChallengeTemplates => Set<ChallengeTemplate>().AsQueryable();

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
        EntityEntry<Athlete> entry = await Set<Athlete>().AddAsync(athlete, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> UpdateAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Set<Athlete>().Update(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Athlete> RemoveAthleteAsync(Athlete athlete, CancellationToken ct = default)
    {
        EntityEntry<Athlete> entry = Set<Athlete>().Remove(athlete);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = await Set<Activity>().AddAsync(activity, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> UpdateActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Set<Activity>().Update(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Activity> RemoveActivityAsync(Activity activity, CancellationToken ct = default)
    {
        EntityEntry<Activity> entry = Set<Activity>().Remove(activity);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> AddChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = await Set<Challenge>().AddAsync(challenge, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeTemplate> AddChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = await Set<ChallengeTemplate>().AddAsync(challengeTemplate, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> UpdateChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = Set<Challenge>().Update(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Challenge> RemoveChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        EntityEntry<Challenge> entry = Set<Challenge>().Remove(challenge);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeTemplate> RemoveChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = Set<ChallengeTemplate>().Remove(challengeTemplate);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> AddFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = await Set<Following>().AddAsync(following, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    public async Task<FollowRequest> AddFollowerRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = await Set<FollowRequest>().AddAsync(followRequest, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<Following> RemoveFollowingAsync(Following following, CancellationToken ct = default)
    {
        EntityEntry<Following> entry = Set<Following>().Remove(following);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<FollowRequest> RemoveFollowRequestAsync(FollowRequest followRequest, CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = Set<FollowRequest>().Remove(followRequest);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeGroup> AddChallengeGroupAsync(ChallengeGroup challengeGroup, CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = await Set<ChallengeGroup>().AddAsync(challengeGroup, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
    public async Task<ChallengeGroup> RemoveChallengeGroupAsync(ChallengeGroup challengeGroup,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = Set<ChallengeGroup>().Remove(challengeGroup);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
}
