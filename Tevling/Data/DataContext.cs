using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tevling.Model.Notification;

namespace Tevling.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Athlete> Athletes { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<Following> Following { get; set; }
    public DbSet<FollowRequest> FollowRequests { get; set; }

    public DbSet<ChallengeGroup> ChallengeGroups { get; set; }
    public DbSet<ChallengeTemplate> ChallengeTemplates { get; set; }

    public DbSet<Notification> UnreadNotifications { get; set; }

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

        modelBuilder.Entity<Notification>()
            .Property(n => n.Created)
            .HasConversion(new DateTimeOffsetToBinaryConverter());
        
        modelBuilder.Entity<Notification>()
            .HasKey(n => n.Id);
        

    }

    public Task InitAsync()
    {
        //Database.EnsureCreatedAsync();
        return Database.MigrateAsync();
    }

    public async Task AddNotificationsAsync(IReadOnlyCollection<Notification> notifications, CancellationToken ct = default)
    {
        await UnreadNotifications.AddRangeAsync(notifications, ct);
        _ = await SaveChangesAsync(ct);
    }

    public async Task RemoveNotifications(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        UnreadNotifications.RemoveRange(UnreadNotifications.Where(n => ids.Contains(n.Id)));
        _ = await SaveChangesAsync(ct);
    }

    public async Task RemoveNotifications(IReadOnlyCollection<Notification> notifications,
        CancellationToken ct = default)
    {
        UnreadNotifications.RemoveRange(notifications);
        _ = await SaveChangesAsync(ct);
    }

    public async Task<ICollection<Notification>> MarkNotificationsAsReadAsync(IReadOnlyCollection<Notification> notifications,
        CancellationToken ct = default)
    {
        List<Notification> notificationsToUpdate =
            [.. UnreadNotifications.AsTracking().Where(n => notifications.Contains(n)).Where(n => n.Read == null)];
        foreach (Notification notification in notificationsToUpdate)
        {
            notification.Read = DateTimeOffset.Now;
        }
        _ = await SaveChangesAsync(ct);
        
        return notificationsToUpdate;
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

    public async Task<ChallengeTemplate> AddChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = await ChallengeTemplates.AddAsync(challengeTemplate, ct);
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

    public async Task<ChallengeTemplate> RemoveChallengeTemplateAsync(ChallengeTemplate challengeTemplate,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeTemplate> entry = ChallengeTemplates.Remove(challengeTemplate);
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

    public async Task<FollowRequest> AddFollowerRequestAsync(FollowRequest followRequest,
        CancellationToken ct = default)
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

    public async Task<FollowRequest> RemoveFollowRequestAsync(FollowRequest followRequest,
        CancellationToken ct = default)
    {
        EntityEntry<FollowRequest> entry = FollowRequests.Remove(followRequest);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeGroup> AddChallengeGroupAsync(ChallengeGroup challengeGroup,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = await ChallengeGroups.AddAsync(challengeGroup, ct);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }

    public async Task<ChallengeGroup> RemoveChallengeGroupAsync(ChallengeGroup challengeGroup,
        CancellationToken ct = default)
    {
        EntityEntry<ChallengeGroup> entry = ChallengeGroups.Remove(challengeGroup);
        _ = await SaveChangesAsync(ct);
        entry.State = EntityState.Detached;
        return entry.Entity;
    }
}
